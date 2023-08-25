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
        (disabled for next update - bish)
       The purpose of this patch is to prevent a autosave from occurring unintentionally during world unload initiated by the !stop or !restart command.
       Due to the user using the command(s) potentally performing a autosave or an opting out of a autosave, Keen's autosave code during unload has to be disabled.
       Setting MySandboxGame.ConfigDedicated.RestartSave to false can resolve this issue, but this method (patch method) prevents us from changing the config file in order to prevent confusion.
    */
    //[PatchShim]
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

            for (var i = 0; i < msil.Count; i++)
            {
                var instruction = msil[i];
                if (instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is MsilOperandInline.MsilOperandReflected<FieldInfo> operandReflected 
                    && operandReflected.Value.FieldType == typeof(bool) && operandReflected.Value.Name == "IsDedicated")
                {
                    for (int c = 0; c < 13; c++)
                    {
                        msil.RemoveAt(i);
                    }

                    var call = new MsilInstruction(OpCodes.Call);
                    (call.Operand as MsilOperandInline.MsilOperandReflected<MethodBase>).Value = typeof(AutoSavePatch).GetMethod(nameof(SaveIfNeeded), BindingFlags.NonPublic | BindingFlags.Static);
                    msil.Insert(i, call);

                    break;
                }
            }

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
