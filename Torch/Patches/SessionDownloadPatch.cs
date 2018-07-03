using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            if (!__result.Checkpoint.Mods.Any(m => m.PublishedFileId == TorchModCore.MOD_ID))
                __result.Checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(TorchModCore.MOD_ID));
        }
    }
}
