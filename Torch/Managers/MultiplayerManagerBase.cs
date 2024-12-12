using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Utils;
using Torch.ViewModels;
using VRage.Game.ModAPI;
using VRage.GameServices;

namespace Torch.Managers
{
    /// <inheritdoc />
    public abstract class MultiplayerManagerBase : Manager, IMultiplayerManagerBase
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public event Action<IPlayer> PlayerJoined;
        /// <inheritdoc />
        public event Action<IPlayer> PlayerLeft;

        public MtObservableSortedDictionary<ulong, PlayerViewModel> Players { get; } = new MtObservableSortedDictionary<ulong, PlayerViewModel>();

#pragma warning disable 649
        [ReflectedGetter(Name = "m_players")]
        private static Func<MyPlayerCollection, ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer>> _onlinePlayers;
#pragma warning restore 649

        protected MultiplayerManagerBase(ITorchBase torch) : base(torch)
        {

        }

        /// <inheritdoc />
        public override void Attach()
        {
            MyMultiplayer.Static.ClientLeft += OnClientLeft;
        }

        /// <inheritdoc />
        public override void Detach()
        {
            if (MyMultiplayer.Static != null)
                MyMultiplayer.Static.ClientLeft -= OnClientLeft;
        }

        /// <inheritdoc />
        public IMyPlayer GetPlayerByName(string name)
        {
            return _onlinePlayers.Invoke(MySession.Static.Players).FirstOrDefault(x => x.Value.DisplayName == name).Value;
        }

        /// <inheritdoc />
        public IMyPlayer GetPlayerBySteamId(ulong steamId)
        {
            _onlinePlayers.Invoke(MySession.Static.Players).TryGetValue(new MyPlayer.PlayerId(steamId), out MyPlayer p);
            return p;
        }

        public ulong GetSteamId(long identityId)
        {
            foreach (KeyValuePair<MyPlayer.PlayerId, MyPlayer> kv in _onlinePlayers.Invoke(MySession.Static.Players))
            {
                if (kv.Value.Identity.IdentityId == identityId)
                    return kv.Key.SteamId;
            }

            return 0;
        }

        /// <inheritdoc />
        public string GetSteamUsername(ulong steamId)
        {
            return MyMultiplayer.Static.GetMemberName(steamId);
        }

        private void OnClientLeft(ulong steamId, MyChatMemberStateChangeEnum stateChange)
        {
            Players.TryGetValue(steamId, out PlayerViewModel vm);
            if (vm == null)
                vm = new PlayerViewModel(steamId);
            _log.Info($"{vm.Name} ({vm.SteamId}) {(ConnectionState)stateChange}.");
            PlayerLeft?.Invoke(vm);
            Players.Remove(steamId);
        }

        protected void RaiseClientJoined(ulong steamId)
        {
            var vm = new PlayerViewModel(steamId) { State = ConnectionState.Connected };
            _log.Info($"Player {vm.Name} joined ({vm.SteamId})");
            Players.Add(steamId, vm);
            PlayerJoined?.Invoke(vm);
        }
    }
}
