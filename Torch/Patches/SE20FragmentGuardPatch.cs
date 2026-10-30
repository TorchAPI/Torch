using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using NLog;
using Sandbox.Engine.Multiplayer;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Patches
{
    [PatchShim]
    internal static class SE20FragmentGuardPatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static Type _receiveQueueType;
        private static int _successValue;
        private static long _droppedCopies;

        public static void Patch(PatchContext context)
        {
            try
            {
                _receiveQueueType = typeof(MyMultiplayerBase).Assembly
                    .GetType("Sandbox.Engine.Networking.MyReceiveQueue", throwOnError: false);
                if (_receiveQueueType == null)
                {
                    _log.Error("SE20FragmentGuardPatch: MyReceiveQueue type not found; fragment guard NOT applied.");
                    return;
                }

                var receiveOne = _receiveQueueType.GetMethod("ReceiveOne",
                    BindingFlags.Public | BindingFlags.Instance, null,
                    new[] { typeof(uint).MakeByRefType() }, null);
                if (receiveOne == null)
                {
                    _log.Error("SE20FragmentGuardPatch: MyReceiveQueue.ReceiveOne not found; fragment guard NOT applied.");
                    return;
                }

                var statusType = _receiveQueueType.GetNestedType("ReceiveStatus",
                    BindingFlags.Public | BindingFlags.NonPublic);
                if (statusType == null || !statusType.IsEnum)
                {
                    _log.Error("SE20FragmentGuardPatch: ReceiveStatus enum not found; fragment guard NOT applied.");
                    return;
                }
                _successValue = Convert.ToInt32(Enum.Parse(statusType, "Success"));

                context.GetPattern(receiveOne).Transpilers.Add(
                    typeof(SE20FragmentGuardPatch).GetMethod(nameof(Transpile),
                        BindingFlags.Static | BindingFlags.NonPublic));
                _log.Info("SE20FragmentGuardPatch: registered transpiler for MyReceiveQueue.ReceiveOne.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "SE20FragmentGuardPatch: failed to register; fragment guard NOT applied.");
            }
        }

        private static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> instructions)
        {
            var msil = instructions.ToList();

            bool copyGuarded = PatchArrayCopy(msil);
            bool deRecursed = PatchRecursion(msil);

            if (!copyGuarded)
                _log.Error("SE20FragmentGuardPatch: Array.Copy anchor not found in ReceiveOne; " +
                           "crash guard NOT applied (method left unchanged).");
            if (!deRecursed)
                _log.Warn("SE20FragmentGuardPatch: recursive ReceiveOne anchor not found; " +
                          "stack-growth guard not applied.");
            if (copyGuarded || deRecursed)
                _log.Info($"SE20FragmentGuardPatch applied (copyGuard={copyGuarded}, deRecurse={deRecursed}).");

            return msil;
        }

        // Retarget the single 5-arg System.Array.Copy call to GuardedCopy. Stack-identical.
        private static bool PatchArrayCopy(List<MsilInstruction> msil)
        {
            var guarded = typeof(SE20FragmentGuardPatch).GetMethod(nameof(GuardedCopy),
                BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var inst in msil)
            {
                if (inst.OpCode != OpCodes.Call && inst.OpCode != OpCodes.Callvirt)
                    continue;
                if (!(inst.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> op) || op.Value == null)
                    continue;
                if (op.Value.Name != "Copy" || op.Value.DeclaringType != typeof(Array))
                    continue;

                var ps = op.Value.GetParameters();
                if (ps.Length == 5 && ps[1].ParameterType == typeof(long) && ps[4].ParameterType == typeof(long))
                {
                    op.Value = guarded;
                    return true;
                }
            }
            return false;
        }

        // Replace the non-final-part self-call (ldarg.0; ldarg.1; call ReceiveOne; ret)
        // with (ldc.i4 Success; ret) so the reader thread never recurses on a frame.
        private static bool PatchRecursion(List<MsilInstruction> msil)
        {
            for (int i = 0; i < msil.Count; i++)
            {
                var inst = msil[i];
                if (inst.OpCode != OpCodes.Call && inst.OpCode != OpCodes.Callvirt)
                    continue;
                if (!(inst.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> op) || op.Value == null)
                    continue;
                if (op.Value.Name != "ReceiveOne" || op.Value.DeclaringType != _receiveQueueType)
                    continue;

                if (i < 2 || i + 1 >= msil.Count)
                    return false;
                if (msil[i - 2].OpCode != OpCodes.Ldarg_0 ||
                    msil[i - 1].OpCode != OpCodes.Ldarg_1 ||
                    msil[i + 1].OpCode != OpCodes.Ret)
                    return false;

                var load = new MsilInstruction(OpCodes.Ldc_I4).InlineValue(_successValue);
                foreach (var label in msil[i - 2].Labels)
                    load.Labels.Add(label);

                msil.RemoveRange(i - 2, 4); // ldarg.0, ldarg.1, call, ret
                msil.Insert(i - 2, load);
                msil.Insert(i - 1, new MsilInstruction(OpCodes.Ret));
                return true;
            }
            return false;
        }

        // Signature matches System.Array.Copy(Array, long, Array, long, long).
        // Copies only when fully in bounds; a mismatch drops the part instead of throwing.
        private static void GuardedCopy(Array src, long srcIndex, Array dst, long dstIndex, long length)
        {
            if (src != null && dst != null &&
                srcIndex >= 0 && dstIndex >= 0 && length >= 0 &&
                srcIndex + length <= src.LongLength &&
                dstIndex + length <= dst.LongLength)
            {
                if (length != 0)
                    Array.Copy(src, srcIndex, dst, dstIndex, length);
                return;
            }

            // Out of bounds: this is the SE-20 fatal input. Drop the part; the
            // incomplete assembly is rejected later per-peer in MyReceiveQueue.Process.
            long n = Interlocked.Increment(ref _droppedCopies);
            if (n == 1 || n % 1000 == 0)
                _log.Warn($"SE20FragmentGuardPatch: dropped {n} out-of-bounds fragment copies (likely malformed/hostile parts).");
        }
    }
}
