using System;
using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using Torch.API;

namespace Torch
{
    /// <summary>
    /// Stores player information in an observable format.
    /// </summary>
    public class Player : ViewModel, IPlayer
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

        //TODO: track identity history
        public List<ulong> IdentityIds { get; } = new List<ulong>();

        public DateTime LastConnected { get; private set; }

        public ConnectionState State
        {
            get { return _state; }
            set { _state = value; OnPropertyChanged(); }
        }

        public Player(ulong steamId)
        {
            _steamId = steamId;
            _name = MyMultiplayer.Static.GetMemberName(steamId);
            _state = ConnectionState.Unknown;
        }

        public void SetConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                LastConnected = DateTime.Now;

            State = state;
        }
    }
}