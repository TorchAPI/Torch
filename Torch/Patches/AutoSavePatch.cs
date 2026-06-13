using NLog;
using Sandbox;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Patches
{
    /* 
       The purpose of this patch is to prevent an autosave from occurring unintentionally during world unload initiated by the !stop false or !restart false commands.
       Keen's autosave code during unload has to be disabled or it bypasses the command.
    */
    [PatchShim]
    internal static class AutoSavePatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// If set to true, specifies that the session is being unloaded from a torch ingame chat command and that saving is being handled by the command.
        /// </summary>
        public static bool SaveFromCommand { get; set; } = false;

        public static void Patch(PatchContext ctx)
        {
            var transpiler = typeof(AutoSavePatch).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static);
            ctx.GetPattern(typeof(MySession).GetMethod("Unload", BindingFlags.Public | BindingFlags.Instance))
               .Transpilers.Add(transpiler);
            _log.Info("Patching autosave on unload.");
        }

        private static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> instructions)
        {
            var msil = instructions.ToList();

            int anchorIdx = -1;
            for (int i = 0; i < msil.Count; i++)
            {
                var inst = msil[i];
                if (inst.OpCode == OpCodes.Ldstr
                    && inst.Operand is MsilOperandInline.MsilOperandString strOp
                    && strOp.Value == "Autosave in unload")
                {
                    anchorIdx = i;
                    break;
                }
            }

            if (anchorIdx < 0)
            {
                _log.Error("AutoSavePatch: 'Autosave in unload' string not found in MySession.Unload. " +
                           "Keen may have changed the log message. Patch skipped.");
                return msil;
            }
			
            int startIdx = -1;
            for (int i = anchorIdx - 1; i >= 0; i--)
            {
                var inst = msil[i];
                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                    && inst.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> getStaticOp
                    && getStaticOp.Value.Name == "get_Static"
                    && getStaticOp.Value.DeclaringType == typeof(MySession))
                {
                    startIdx = i;
                    break;
                }
            }

            int endIdx = -1;
            for (int i = anchorIdx + 1; i < msil.Count; i++)
            {
                var inst = msil[i];
                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                    && inst.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> methOp
                    && methOp.Value.Name == "Save"
                    && methOp.Value.DeclaringType == typeof(MySession))
                {
                    endIdx = i;
                    break;
                }
            }

            if (endIdx >= 0 && endIdx + 1 < msil.Count && msil[endIdx + 1].OpCode == OpCodes.Pop)
            {
                endIdx++;
            }

            if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
            {
                _log.Error("AutoSavePatch: could not locate the autosave block boundaries " +
                           $"(startIdx={startIdx}, anchorIdx={anchorIdx}, endIdx={endIdx}). Patch skipped.");
                return msil;
            }

            int count = endIdx - startIdx + 1;
            msil.RemoveRange(startIdx, count);

            var call = new MsilInstruction(OpCodes.Call);
            (call.Operand as MsilOperandInline.MsilOperandReflected<MethodBase>).Value =
                typeof(AutoSavePatch).GetMethod(nameof(SaveIfNeeded), BindingFlags.NonPublic | BindingFlags.Static);
            msil.Insert(startIdx, call);

            _log.Info($"AutoSavePatch: patched MySession.Unload (replaced {count} instructions).");
            return msil;
        }

        // Reimplementation of Keen's autosaving code during world unload with SaveFromCommand check and with save status being outputed to console.
        private static void SaveIfNeeded()
        {
            if (SaveFromCommand)
            {
                return;
            }

            if (Sandbox.Engine.Platform.Game.IsDedicated && MySandboxGame.ConfigDedicated.RestartSave)
            {
                _log.Info("Autosaving during world unloading.");
                // We have to use the vanilla implementation as the torch implementation does not work in Sandbox.Game.World.MySession:Unload()
                bool result = MySession.Static.Save();

                if (result)
                {
                    _log.Info("Autosave successful.");
                }
                else
                {
                    _log.Warn("Autosave failed.");
                }
            }
        }
    }
}
