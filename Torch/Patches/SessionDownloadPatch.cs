using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Mod;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    internal static class SessionDownloadPatch
    {
        internal static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MySession).GetMethod(nameof(MySession.GetWorld))).Suffixes.Add(typeof(SessionDownloadPatch).GetMethod(nameof(SuffixGetWorld), BindingFlags.Static | BindingFlags.NonPublic));
        }

        // ReSharper disable once InconsistentNaming
        private static void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            //copy this list so mods added here don't propagate up to the real session
            __result.Checkpoint.Mods = __result.Checkpoint.Mods.ToList();
            
            __result.Checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(TorchModCore.MOD_ID));
        }
    }
}
