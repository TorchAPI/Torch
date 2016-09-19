using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Piston;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using VRage.Library.Collections;

namespace Piston.Server
{
    /// <summary>
    /// Provides a proxy to the game's multiplayer-related functions.
    /// </summary>
    public class MultiplayerManager
    {
        public event Action<PlayerInfo> PlayerJoined;
        public event Action<PlayerInfo> PlayerLeft;
        public event Action<ChatItemInfo> ChatMessageReceived;

        public MTObservableCollection<PlayerInfo> PlayersView { get; } = new MTObservableCollection<PlayerInfo>();
        public MTObservableCollection<ChatItemInfo> ChatView { get; } = new MTObservableCollection<ChatItemInfo>();
        public PlayerInfo LocalPlayer { get; private set; }

        internal MultiplayerManager(ServerManager serverManager)
        {
            serverManager.SessionLoaded += OnSessionLoaded;
        }

        public void KickPlayer(ulong steamId) => MyMultiplayer.Static.KickClient(steamId);

        public void BanPlayer(ulong steamId, bool banned = true) => MyMultiplayer.Static.BanClient(steamId, banned);

        /// <summary>
        /// Send a message in chat.
        /// </summary>
        public void SendMessage(string message)
        {
            MyMultiplayer.Static.SendChatMessage(message);
            ChatView.Add(new ChatItemInfo(LocalPlayer, message));
        }

        private void OnSessionLoaded()
        {
            LocalPlayer = new PlayerInfo(MyMultiplayer.Static.ServerId) { Name = "Server", State = ConnectionState.Connected };

            MyMultiplayer.Static.ChatMessageReceived += OnChatMessage;
            MyMultiplayer.Static.ClientKicked += OnClientKicked;
            MyMultiplayer.Static.ClientLeft += OnClientLeft;
            MySession.Static.Players.PlayerRequesting += OnPlayerRequesting;
        }

        private void OnChatMessage(ulong steamId, string message, ChatEntryTypeEnum chatType)
        {
            var player = PlayersView.FirstOrDefault(p => p.SteamId == steamId);
            if (player == null || player == LocalPlayer)
                return;

            var info = new ChatItemInfo(player, message);
            ChatView.Add(info);
            ChatMessageReceived?.Invoke(info);
        }

        /// <summary>
        /// Invoked when a client logs in and hits the respawn screen.
        /// </summary>
        private void OnPlayerRequesting(PlayerRequestArgs args)
        {
            var steamId = args.PlayerId.SteamId;
            var player = new PlayerInfo(steamId) {State = ConnectionState.Connected};
            PlayersView.Add(player);
            PlayerJoined?.Invoke(player);
            
        }

        private void OnClientKicked(ulong steamId)
        {
            OnClientLeft(steamId, ChatMemberStateChangeEnum.Kicked);
        }

        private void OnClientLeft(ulong steamId, ChatMemberStateChangeEnum stateChange)
        {
            var player = PlayersView.FirstOrDefault(p => p.SteamId == steamId);

            if (player == null)
                return;

            player.State = (ConnectionState)stateChange;
            PlayersView.Remove(player);
            PlayerLeft?.Invoke(player);
        }
    }

    /// <summary>
    /// Stores player information in an observable format.
    /// </summary>
    public class PlayerInfo : ObservableType
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

    public class ChatItemInfo : ObservableType
    {
        private PlayerInfo _sender;
        private string _message;
        private DateTime _timestamp;

        public PlayerInfo Sender
        {
            get { return _sender; }
            set { _sender = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string Time => Timestamp.ToShortTimeString();

        public ChatItemInfo(PlayerInfo sender, string message)
        {
            _sender = sender;
            _message = message;
            _timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Identifies a player's current connection state.
    /// </summary>
    [Flags]
    public enum ConnectionState
    {
        Unknown,
        Connected = 1,
        Left = 2,
        Disconnected = 4,
        Kicked = 8,
        Banned = 16,
    }
}
