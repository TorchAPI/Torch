using Sandbox.Engine.Multiplayer;

namespace Torch
{
    /// <summary>
    /// Stores player information in an observable format.
    /// </summary>
    public class PlayerInfo : ViewModel
    {
        private ulong _steamId;
        private string _name;
        private ConnectionState _state;

        public ulong SteamId
        {
            get { return _steamId; }
            set { _steamId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public ConnectionState State
        {
            get { return _state; }
            set { _state = value; OnPropertyChanged(); }
        }

        public PlayerInfo(ulong steamId)
        {
            _steamId = steamId;
            _name = MyMultiplayer.Static.GetMemberName(steamId);
            _state = ConnectionState.Unknown;
        }
    }
}