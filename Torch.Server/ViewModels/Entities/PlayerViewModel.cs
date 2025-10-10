using System;
using System.Collections.Generic;
using Sandbox.Game.World;

namespace Torch.Server.ViewModels.Entities
{
    public class PlayerViewModel : ViewModel
    {
        private readonly MyIdentity _backing;
        private readonly MyPlayer.PlayerId _backingPlayer;

        public PlayerViewModel()
        { }

        public MyIdentity Player => _backing;

        public PlayerViewModel(MyIdentity player, MyPlayer.PlayerId playerId)
        {
            _backing = player;
            _backingPlayer = playerId;
        }

        public string Name => Player.DisplayName;
        public long ID => Player.IdentityId;
        public ulong SteamID => _backingPlayer.SteamId;

        public string FactionTag
        {
            get
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(ID);
                return faction is null ? string.Empty : faction.Tag;
            }
        }

        public string FactionName 
        {
            get
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(ID);
                return faction is null ? string.Empty : faction.Name;
            }
        }
        public DateTime LastLogin => Player.LastLoginTime;
        public DateTime LastLogout => Player.LastLogoutTime;
        public string LastDeathLocation => Player.LastDeathPosition.ToString();
        public int BlocksBuilt => Player.BlockLimits.BlocksBuilt;
        public int PCU => Player.BlockLimits.PCUBuilt;
        public bool OverLimits => Player.BlockLimits.IsOverLimits;
    }
}