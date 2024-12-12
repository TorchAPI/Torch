using System;
using System.Reflection;
using NLog;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;
using Torch.API.Managers;
using Torch.Server.Managers;

namespace Torch.Patches
{
    [PatchShim]
    internal static class PromotePatch
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private static IMultiplayerManagerServer _backing;

        private static IMultiplayerManagerServer ServerManager => _backing ?? (_backing = TorchBase.Instance?.CurrentSession?.Managers.GetManager<IMultiplayerManagerServer>());

        public static void Patch(PatchContext ctx)
        {
            _log.Info("patching promote");
            ctx.GetPattern(typeof(MySession).GetMethod("OnPromoteLevelSet", BindingFlags.NonPublic | BindingFlags.Static)).Prefixes.Add(typeof(PromotePatch).GetMethod(nameof(PromotePrefix)));
        }

        public static void PromotePrefix(ulong steamId, MyPromoteLevel level)
        {
            if (ServerManager is MultiplayerManagerDedicated d)
                d.RaisePromoteChanged(steamId, level);
            else
                throw new NotSupportedException();
        }
    }
}
