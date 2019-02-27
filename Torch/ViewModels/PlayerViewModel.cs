using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using VRage.Game.ModAPI;
using VRage.Replication;

namespace Torch.ViewModels
{
    public class PlayerViewModel : ViewModel, IPlayer
    {
        public ulong SteamId { get; }
        public string Name { get; }
        private ConnectionState _state;
        public ConnectionState State { get => _state; set { _state = value; OnPropertyChanged(); } }
        public MyPromoteLevel PromoteLevel => MySession.Static.GetUserPromoteLevel(SteamId);

        public string PromotedName
        {
            get
            {
                var p = PromoteLevel;
                if (p <= MyPromoteLevel.None)
                    return Name;
                else
                    return $"{Name} ({p})";
            }
        }

        public PlayerViewModel(ulong steamId, string name = null)
        {
            SteamId = steamId;
            Name = name ?? MySession.Static.Players.TryGetPlayerName(steamId);
        }
    }
}
