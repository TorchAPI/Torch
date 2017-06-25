using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamSDK;
using Torch.API;

namespace Torch.ViewModels
{
    public class PlayerViewModel : ViewModel, IPlayer
    {
        public ulong SteamId { get; }
        public string Name { get; }
        private ConnectionState _state;
        public ConnectionState State { get => _state; set { _state = value; OnPropertyChanged(); } }

        public PlayerViewModel(ulong steamId, string name = null)
        {
            SteamId = steamId;
            Name = name ?? SteamAPI.Instance?.Friends?.GetPersonaName(steamId) ?? "???";
        }
    }
}
