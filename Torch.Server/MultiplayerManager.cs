using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using Torch.Server.ViewModels;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace Torch.Server
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

        private TorchServer _server;

        internal MultiplayerManager(TorchServer server)
        {
            _server = server;
            _server.Server.SessionLoaded += OnSessionLoaded;
        }

        public void KickPlayer(ulong steamId) => _server.Server.BeginGameAction(() => MyMultiplayer.Static.KickClient(steamId));

        public void BanPlayer(ulong steamId, bool banned = true)
        {
            _server.Server.BeginGameAction(() =>
            {
                MyMultiplayer.Static.BanClient(steamId, banned);
                if (_gameOwnerIds.ContainsKey(steamId))
                    MyMultiplayer.Static.BanClient(_gameOwnerIds[steamId], banned);
            });
        }

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

            //TODO: Move these with the methods?
            RemoveHandlers();
            SteamSDK.SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse += ValidateAuthTicketResponse;
            SteamSDK.SteamServerAPI.Instance.GameServer.UserGroupStatus += UserGroupStatus;
            _members = (List<ulong>)typeof(MyDedicatedServerBase).GetField("m_members", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);
            _waitingForGroup = (HashSet<ulong>)typeof(MyDedicatedServerBase).GetField("m_waitingForGroup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);
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

        //TODO: Split the following into a new file?
        //These methods override some Keen code to allow us full control over client authentication.
        //This lets us have a server set to private (admins only) or friends (friends of all listed admins)
        private List<ulong> _members;
        private HashSet<ulong> _waitingForGroup;
        private HashSet<ulong> _waitingForFriends;
        private Dictionary<ulong, ulong> _gameOwnerIds = new Dictionary<ulong, ulong>();
        /// <summary>
        /// Removes Keen's hooks into some Steam events so we have full control over client authentication
        /// </summary>
        private static void RemoveHandlers()
        {
            var eventField = typeof(GameServer).GetField("<backing_store>ValidateAuthTicketResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            var eventDel = eventField?.GetValue(SteamServerAPI.Instance.GameServer) as MulticastDelegate;
            if (eventDel != null)
            {
                foreach (var handle in eventDel.GetInvocationList())
                {
                    if (handle.Method.Name == "GameServer_ValidateAuthTicketResponse")
                    {
                        SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse -= handle as ValidateAuthTicketResponse;
                    }
                }
            }
            eventField = typeof(GameServer).GetField("<backing_store>UserGroupStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            eventDel = eventField?.GetValue(SteamServerAPI.Instance.GameServer) as MulticastDelegate;
            if (eventDel != null)
            {
                foreach (var handle in eventDel.GetInvocationList())
                {
                    if (handle.Method.Name == "GameServer_UserGroupStatus")
                    {
                        SteamServerAPI.Instance.GameServer.UserGroupStatus -= handle as UserGroupStatus;
                    }
                }
            }
        }

        //Largely copied from SE
        private void ValidateAuthTicketResponse(ulong steamID, AuthSessionResponseEnum response, ulong ownerSteamID)
        {
            MyLog.Default.WriteLineAndConsole($"{Logger.Prefix} Server ValidateAuthTicketResponse ({response}), owner: {ownerSteamID}");

            if (steamID != ownerSteamID)
            {
                MyLog.Default.WriteLineAndConsole($"User {steamID} is using a game owned by {ownerSteamID}. Tracking...");
                _gameOwnerIds[steamID] = ownerSteamID;
                
                if (MySandboxGame.ConfigDedicated.Banned.Contains(ownerSteamID))
                {
                    MyLog.Default.WriteLineAndConsole($"Game owner {ownerSteamID} is banned. Banning and rejecting client {steamID}...");
                    UserRejected(steamID, JoinResult.BannedByAdmins);
                    BanPlayer(steamID, true);
                }
            }

            if (response == AuthSessionResponseEnum.OK)
            {
                if (MySession.Static.MaxPlayers > 0 && _members.Count - 1 >= MySession.Static.MaxPlayers) 
                {
                    UserRejected(steamID, JoinResult.ServerFull);
                }
                else if (MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) || MySandboxGame.ConfigDedicated.Administrators.Contains(MyDedicatedServerBase.ConvertSteamIDFrom64(steamID)))
                {
                    UserAccepted(steamID);
                }
                else if (MySandboxGame.ConfigDedicated.GroupID == 0)
                {
                    switch (MySession.Static.OnlineMode)
                    {
                        case MyOnlineModeEnum.PUBLIC:
                            UserAccepted(steamID);
                            break;
                        case MyOnlineModeEnum.PRIVATE:
                            UserRejected(steamID, JoinResult.NotInGroup);
                            break;
                        case MyOnlineModeEnum.FRIENDS:
                            //TODO: actually verify friendship
                            UserRejected(steamID, JoinResult.NotInGroup);
                            break;
                    }
                }
                else if (SteamServerAPI.Instance.GetAccountType(MySandboxGame.ConfigDedicated.GroupID) != AccountType.Clan)
                {
                    UserRejected(steamID, JoinResult.GroupIdInvalid);
                }
                else if (SteamServerAPI.Instance.GameServer.RequestGroupStatus(steamID, MySandboxGame.ConfigDedicated.GroupID))
                {
                    // Returns false when there's no connection to Steam
                    _waitingForGroup.Add(steamID);
                }
                else
                {
                    UserRejected(steamID, JoinResult.SteamServersOffline);
                }
            }
            else
            {
                JoinResult joinResult = JoinResult.TicketInvalid;
                switch (response)
                {
                    case AuthSessionResponseEnum.AuthTicketCanceled:
                        joinResult = JoinResult.TicketCanceled;
                        break;
                    case AuthSessionResponseEnum.AuthTicketInvalidAlreadyUsed:
                        joinResult = JoinResult.TicketAlreadyUsed;
                        break;
                    case AuthSessionResponseEnum.LoggedInElseWhere:
                        joinResult = JoinResult.LoggedInElseWhere;
                        break;
                    case AuthSessionResponseEnum.NoLicenseOrExpired:
                        joinResult = JoinResult.NoLicenseOrExpired;
                        break;
                    case AuthSessionResponseEnum.UserNotConnectedToSteam:
                        joinResult = JoinResult.UserNotConnected;
                        break;
                    case AuthSessionResponseEnum.VACBanned:
                        joinResult = JoinResult.VACBanned;
                        break;
                    case AuthSessionResponseEnum.VACCheckTimedOut:
                        joinResult = JoinResult.VACCheckTimedOut;
                        break;
                }

                UserRejected(steamID, joinResult);
            }
        }

        private void UserGroupStatus(ulong userId, ulong groupId, bool member, bool officer)
        {
            if (groupId == MySandboxGame.ConfigDedicated.GroupID && _waitingForGroup.Remove(userId))
            {
                if (member || officer)
                {
                    UserAccepted(userId);
                }
                else
                {
                    UserRejected(userId, JoinResult.NotInGroup);
                }
            }
        }

        private void UserAccepted(ulong steamId)
        {
            //TODO: Raise user joined event here
            typeof(MyDedicatedServerBase).GetMethod("UserAccepted", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId});
        }

        private void UserRejected(ulong steamId, JoinResult reason)
        {
            typeof(MyDedicatedServerBase).GetMethod("UserRejected", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId, reason});
        }
    }
}
