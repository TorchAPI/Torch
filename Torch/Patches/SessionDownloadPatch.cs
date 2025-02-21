using System.Linq;
using System.Reflection;
using Sandbox.Game.World;
using Torch.API.Session;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    internal static class SessionDownloadPatch
    {
        private static ITorchSessionManager _sessionManager;
        private static ITorchSessionManager SessionManager => _sessionManager ?? (_sessionManager = TorchBase.Instance.Managers.GetManager<ITorchSessionManager>());


        internal static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MySession).GetMethod(nameof(MySession.GetWorld))).Suffixes.Add(typeof(SessionDownloadPatch).GetMethod(nameof(SuffixGetWorld), BindingFlags.Static | BindingFlags.NonPublic));
        }

        // ReSharper disable once InconsistentNaming
        private static void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            //copy this list so mods added here don't propagate up to the real session
            __result.Checkpoint.Mods = __result.Checkpoint.Mods.ToList();
            
            __result.Checkpoint.Mods.AddRange(SessionManager.OverrideMods);
        }
    }
}
