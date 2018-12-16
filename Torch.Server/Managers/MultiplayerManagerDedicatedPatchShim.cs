using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Torch.Managers.PatchManager;
using Torch.API.Managers;

namespace Torch.Server.Managers
{
    [PatchShim]
    internal static class MultiplayerManagerDedicatedPatchShim
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyDedicatedServerBase).GetMethod(nameof(MyDedicatedServerBase.BanClient))).Prefixes.Add(typeof(MultiplayerManagerDedicatedPatchShim).GetMethod(nameof(BanPrefix)));
        }

        public static void BanPrefix(ulong userId, bool banned)
        {
            Log.Info($"Caught ban event for {userId}: {banned}");
            TorchBase.Instance.CurrentSession.Managers.GetManager<MultiplayerManagerDedicated>().RaiseClientBanned(userId, banned);
        }
    }
}
