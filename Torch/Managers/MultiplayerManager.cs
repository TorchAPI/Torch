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
using NLog;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using Torch.API;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Commands;
using Torch.ViewModels;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace Torch.Managers
{
    /// <summary>
    /// Provides a proxy to the game's multiplayer-related functions.
    /// </summary>
    public class MultiplayerManager : Manager, IMultiplayerManager
    {
        public event Action<IPlayer> PlayerJoined;
        public event Action<IPlayer> PlayerLeft;
        public event MessageReceivedDel MessageReceived;

        public MTObservableCollection<IChatMessage> ChatHistory { get; } = new MTObservableCollection<IChatMessage>();
        public ObservableDictionary<ulong, PlayerViewModel> Players { get; } = new ObservableDictionary<ulong, PlayerViewModel>();
        public IMyPlayer LocalPlayer => MySession.Static.LocalHumanPlayer;
        private static readonly Logger _log = LogManager.GetLogger(nameof(MultiplayerManager));
        private static readonly Logger _chatLog = LogManager.GetLogger("Chat");
        private Dictionary<MyPlayer.PlayerId, MyPlayer> _onlinePlayers;

        internal MultiplayerManager(ITorchBase torch) : base(torch)
        {
            
        }

        /// <inheritdoc />
        public override void Init()
        {
            Torch.SessionLoaded += OnSessionLoaded;
            Torch.GetManager<ChatManager>().MessageRecieved += Instance_MessageRecieved;
        }

        private void Instance_MessageRecieved(ChatMsg msg, ref bool sendToOthers)
        {
            var message = ChatMessage.FromChatMsg(msg);
            ChatHistory.Add(message);
            _chatLog.Info($"{message.Name}: {message.Message}");
            MessageReceived?.Invoke(message, ref sendToOthers);
        }

        public void KickPlayer(ulong steamId) => Torch.Invoke(() => MyMultiplayer.Static.KickClient(steamId));

        public void BanPlayer(ulong steamId, bool banned = true)
        {
            Torch.Invoke(() =>
            {
                MyMultiplayer.Static.BanClient(steamId, banned);
                if (_gameOwnerIds.ContainsKey(steamId))
                    MyMultiplayer.Static.BanClient(_gameOwnerIds[steamId], banned);
            });
        }

        public IMyPlayer GetPlayerByName(string name)
        {
            ValidateOnlinePlayersList();
            return _onlinePlayers.FirstOrDefault(x => x.Value.DisplayName == name).Value;
        }

        public IMyPlayer GetPlayerBySteamId(ulong steamId)
        {
            ValidateOnlinePlayersList();
            _onlinePlayers.TryGetValue(new MyPlayer.PlayerId(steamId), out MyPlayer p);
            return p;
        }

        public string GetSteamUsername(ulong steamId)
        {
            return MyMultiplayer.Static.GetMemberName(steamId);
        }

        /// <summary>
        /// Send a message in chat.
        /// </summary>
        public void SendMessage(string message, string author = "Server", long playerId = 0, string font = MyFontEnum.Red)
        {
            ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", message));
            var commands = Torch.GetManager<CommandManager>();
            if (commands.IsCommand(message))
            {
                var response = commands.HandleCommandFromServer(message);
                ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", response));
            }
            else
            {
                var msg = new ScriptedChatMsg { Author = author, Font = font, Target = playerId, Text = message };
                MyMultiplayerBase.SendScriptedChatMessage(ref msg);
            }
        }

        private void ValidateOnlinePlayersList()
        {
            if (_onlinePlayers == null)
                _onlinePlayers = MySession.Static.Players.GetPrivateField<Dictionary<MyPlayer.PlayerId, MyPlayer>>("m_players");
        }

        private void OnSessionLoaded()
        {
            _log.Info("Initializing Steam auth");
            MyMultiplayer.Static.ClientKicked += OnClientKicked;
            MyMultiplayer.Static.ClientLeft += OnClientLeft;

            ValidateOnlinePlayersList();

            //TODO: Move these with the methods?
            RemoveHandlers();
            SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse += ValidateAuthTicketResponse;
            SteamServerAPI.Instance.GameServer.UserGroupStatus += UserGroupStatus;
            _members = (List<ulong>)typeof(MyDedicatedServerBase).GetField("m_members", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);
            _waitingForGroup = (HashSet<ulong>)typeof(MyDedicatedServerBase).GetField("m_waitingForGroup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MyMultiplayer.Static);
            _log.Info("Steam auth initialized");
        }

        private void OnClientKicked(ulong steamId)
        {
            OnClientLeft(steamId, ChatMemberStateChangeEnum.Kicked);
        }

        private void OnClientLeft(ulong steamId, ChatMemberStateChangeEnum stateChange)
        {
            Players.TryGetValue(steamId, out PlayerViewModel vm);
            if (vm == null)
                vm = new PlayerViewModel(steamId);
            _log.Info($"{vm.Name} ({vm.SteamId}) {(ConnectionState)stateChange}.");
            PlayerLeft?.Invoke(vm);
            Players.Remove(steamId);
        }

        //TODO: Split the following into a new file?
        //These methods override some Keen code to allow us full control over client authentication.
        //This lets us have a server set to private (admins only) or friends (friends of all listed admins)
        private List<ulong> _members;
        private HashSet<ulong> _waitingForGroup;
        //private HashSet<ulong> _waitingForFriends;
        private Dictionary<ulong, ulong> _gameOwnerIds = new Dictionary<ulong, ulong>();
        //private IMultiplayer _multiplayerImplementation;

        /// <summary>
        /// Removes Keen's hooks into some Steam events so we have full control over client authentication
        /// </summary>
        private static void RemoveHandlers()
        {
            var eventField = typeof(GameServer).GetField("<backing_store>ValidateAuthTicketResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            if (eventField?.GetValue(SteamServerAPI.Instance.GameServer) is MulticastDelegate eventDel)
            {
                foreach (var handle in eventDel.GetInvocationList())
                {
                    if (handle.Method.Name == "GameServer_ValidateAuthTicketResponse")
                    {
                        SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse -= handle as ValidateAuthTicketResponse;
                        _log.Debug("Removed GameServer_ValidateAuthTicketResponse");
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
                        _log.Debug("Removed GameServer_UserGroupStatus");
                    }
                }
            }
        }

        //Largely copied from SE
        private void ValidateAuthTicketResponse(ulong steamID, AuthSessionResponseEnum response, ulong ownerSteamID)
        {
            _log.Info($"Server ValidateAuthTicketResponse ({response}), owner: {ownerSteamID}");

            if (steamID != ownerSteamID)
            {
                _log.Info($"User {steamID} is using a game owned by {ownerSteamID}. Tracking...");
                _gameOwnerIds[steamID] = ownerSteamID;
                
                if (MySandboxGame.ConfigDedicated.Banned.Contains(ownerSteamID))
                {
                    _log.Info($"Game owner {ownerSteamID} is banned. Banning and rejecting client {steamID}...");
                    UserRejected(steamID, JoinResult.BannedByAdmins);
                    BanPlayer(steamID);
                }
            }

            if (response == AuthSessionResponseEnum.OK)
            {
                if (MySession.Static.MaxPlayers > 0 && _members.Count - 1 >= MySession.Static.MaxPlayers) 
                {
                    UserRejected(steamID, JoinResult.ServerFull);
                }
                else if (MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) /*|| MySandboxGame.ConfigDedicated.Administrators.Contains(MyDedicatedServerBase.ConvertSteamIDFrom64(steamID))*/)
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
            typeof(MyDedicatedServerBase).GetMethod("UserAccepted", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId});
            var vm = new PlayerViewModel(steamId) {State = ConnectionState.Connected};
            _log.Info($"Player {vm.Name} joined ({vm.SteamId})");
            Players.Add(steamId, vm);
            PlayerJoined?.Invoke(vm);
        }

        private void UserRejected(ulong steamId, JoinResult reason)
        {
            typeof(MyDedicatedServerBase).GetMethod("UserRejected", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MyMultiplayer.Static, new object[] {steamId, reason});
        }
    }
}
