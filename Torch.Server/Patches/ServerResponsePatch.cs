using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Sandbox.Engine.Multiplayer;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Patches
{
    [PatchShim]
    public static class ServerResponsePatch
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        public static void Patch(PatchContext ctx)
        {
            var transpiler = typeof(ServerResponsePatch).GetMethod(nameof(Transpile), BindingFlags.Public | BindingFlags.Static);
            ctx.GetPattern(typeof(MyDedicatedServerBase).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance))
               .Transpilers.Add(transpiler);
            _log.Info("Patching Steam response polling");
        }

        public static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> instructions)
        {
            // Reduce response timeout from 100 seconds to 5 seconds.
            foreach (var instruction in instructions)
            {
                if (instruction.OpCode == OpCodes.Ldc_I4 && instruction.Operand is MsilOperandInline.MsilOperandInt32 inlineI32 && inlineI32.Value == 1000)
                {
                    _log.Info("Patching Steam response timeout to 5 seconds");
                    inlineI32.Value = 50;
                }

                yield return instruction;
            }
        }
    }
}