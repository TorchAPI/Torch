using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using VRage.Game.ModAPI;

namespace Torch.Client.Manager
{
    public class MultiplayerManagerLobby : MultiplayerManagerBase, IMultiplayerManagerServer
    {
        /// <inheritdoc />
        public IReadOnlyList<ulong> BannedPlayers => new List<ulong>();

        /// <inheritdoc />
        public MultiplayerManagerLobby(ITorchBase torch) : base(torch) { }

        /// <inheritdoc />
        public void KickPlayer(ulong steamId) => Torch.Invoke(() => MyMultiplayer.Static.KickClient(steamId));

        /// <inheritdoc />
        public void BanPlayer(ulong steamId, bool banned = true) => Torch.Invoke(() => MyMultiplayer.Static.BanClient(steamId, banned));

        /// <inheritdoc />
        public void PromoteUser(ulong steamId)
        {
            Torch.Invoke(() =>
            {
                var p = MySession.Static.GetUserPromoteLevel(steamId);
                if (p < MyPromoteLevel.Admin) //cannot promote to owner by design
                    MySession.Static.SetUserPromoteLevel(steamId, p + 1);
            });
        }

        /// <inheritdoc />
        public void DemoteUser(ulong steamId)
        {
            Torch.Invoke(() =>
            {
                var p = MySession.Static.GetUserPromoteLevel(steamId);
                if (p > MyPromoteLevel.None && p < MyPromoteLevel.Owner) //owner cannot be demoted by design
                    MySession.Static.SetUserPromoteLevel(steamId, p - 1);
            });
        }

        /// <inheritdoc />
        public MyPromoteLevel GetUserPromoteLevel(ulong steamId)
        {
            return MySession.Static.GetUserPromoteLevel(steamId);
        }

        /// <inheritdoc />
        public bool IsBanned(ulong steamId) => false;

        /// <inheritdoc />
        public event Action<ulong> PlayerKicked
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public event Action<ulong, bool> PlayerBanned
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public event Action<ulong, MyPromoteLevel> PlayerPromoted
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

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
