using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Sandbox;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;

namespace Torch.Patches
{
    /// <summary>
    /// Patches MySandboxGame.InitQuickLaunch to rethrow exceptions caught during session load.
    /// </summary>
    [PatchShim]
    public static class WorldLoadExceptionPatch
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        [ReflectedMethodInfo(typeof(MySandboxGame), "InitQuickLaunch")]
        private static MethodInfo _quickLaunchMethod = null!;

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(_quickLaunchMethod).AddTranspiler(nameof(Transpile));
        }

        private static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> method)
        {
            var msil = method.ToList();
            for (var i = 0; i < msil.Count; i++)
            {
                var instruction = msil[i];
                if (instruction.IsLocalStore() && instruction.Operand is MsilOperandInline.MsilOperandLocal {Value.Index: 19} operand)
                {
                    msil.InsertRange(i + 1, new []
                    {
                        operand.Instruction.CopyWith(OpCodes.Ldloc_S),
                        new MsilInstruction(OpCodes.Call).InlineValue(new Action<Exception>(LogFatal).Method)
                    });
                }
            }
            return msil;
        }

        private static void LogFatal(Exception e) => Log.Fatal(e.ToStringDemystified());
    }
}