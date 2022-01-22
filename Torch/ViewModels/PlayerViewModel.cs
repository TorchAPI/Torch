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
        public ConnectionState State { get; set; }
        public MyPromoteLevel PromoteLevel => MySession.Static.GetUserPromoteLevel(SteamId);

        public string PromotedName
        {
            get
            {
                var p = PromoteLevel;
                return p <= MyPromoteLevel.None ? Name : $"{Name} ({p})";
            }
        }

        public PlayerViewModel(ulong steamId, string name = null)
        {
            SteamId = steamId;
            Name = name ?? ((MyDedicatedServerBase)MyMultiplayerMinimalBase.Instance).GetMemberName(steamId);
        }
    }
}
