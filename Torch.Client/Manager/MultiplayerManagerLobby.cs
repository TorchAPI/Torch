using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;

namespace Torch.Client.Manager
{
    public class MultiplayerManagerLobby : MultiplayerManagerBase
    {
        /// <inheritdoc />
        public MultiplayerManagerLobby(ITorchBase torch) : base(torch) { }

        /// <inheritdoc />
        public void KickPlayer(ulong steamId) => Torch.Invoke(() => MyMultiplayer.Static.KickClient(steamId));

        /// <inheritdoc />
        public void BanPlayer(ulong steamId, bool banned = true) => Torch.Invoke(() => MyMultiplayer.Static.BanClient(steamId, banned));

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            MyMultiplayer.Static.ClientJoined += RaiseClientJoined;
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            MyMultiplayer.Static.ClientJoined -= RaiseClientJoined;
            base.Detach();
        }
    }
}
