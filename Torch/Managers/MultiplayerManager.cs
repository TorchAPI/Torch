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
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using Torch.API;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Commands;
using Torch.Utils;
using Torch.ViewModels;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Steam;
using VRage.Utils;

namespace Torch.Managers
{
    /// <inheritdoc />
    public class MultiplayerManager : Manager, IMultiplayerManager
    {
        /// <inheritdoc />
        public event Action<IPlayer> PlayerJoined;
        /// <inheritdoc />
        public event Action<IPlayer> PlayerLeft;
        /// <inheritdoc />
        public event MessageReceivedDel MessageReceived;

        public IList<IChatMessage> ChatHistory { get; } = new ObservableList<IChatMessage>();
        public ObservableDictionary<ulong, PlayerViewModel> Players { get; } = new ObservableDictionary<ulong, PlayerViewModel>();
        public List<ulong> BannedPlayers => MySandboxGame.ConfigDedicated.Banned;
        public IMyPlayer LocalPlayer => MySession.Static.LocalHumanPlayer;
        private static readonly Logger Log = LogManager.GetLogger(nameof(MultiplayerManager));
        private static readonly Logger ChatLog = LogManager.GetLogger("Chat");

        [ReflectedGetter(Name = "m_players")]
        private static Func<MyPlayerCollection, Dictionary<MyPlayer.PlayerId, MyPlayer>> _onlinePlayers;

        [Dependency]
        private ChatManager _chatManager;
        [Dependency]
        private CommandManager _commandManager;
        [Dependency]
        private NetworkManager _networkManager;

        internal MultiplayerManager(ITorchBase torch) : base(torch)
        {

        }

        /// <inheritdoc />
        public override void Attach()
        {
            Torch.SessionLoaded += OnSessionLoaded;
            _chatManager.MessageRecieved += Instance_MessageRecieved;
        }

        private void Instance_MessageRecieved(ChatMsg msg, ref bool sendToOthers)
        {
            var message = ChatMessage.FromChatMsg(msg);
            ChatHistory.Add(message);
            ChatLog.Info($"{message.Name}: {message.Message}");
            MessageReceived?.Invoke(message, ref sendToOthers);
        }

        /// <inheritdoc />
        public void KickPlayer(ulong steamId) => Torch.Invoke(() => MyMultiplayer.Static.KickClient(steamId));

        /// <inheritdoc />
        public void BanPlayer(ulong steamId, bool banned = true)
        {
            Torch.Invoke(() =>
            {
                MyMultiplayer.Static.BanClient(steamId, banned);
                if (_gameOwnerIds.ContainsKey(steamId))
                    MyMultiplayer.Static.BanClient(_gameOwnerIds[steamId], banned);
            });
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
            foreach (var kv in _onlinePlayers.Invoke(MySession.Static.Players))
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

        /// <inheritdoc />
        public void SendMessage(string message, string author = "Server", long playerId = 0, string font = MyFontEnum.Red)
        {
            if (string.IsNullOrEmpty(message))
                return;

            ChatHistory.Add(new ChatMessage(DateTime.Now, 0, author, message));
            if (_commandManager.IsCommand(message))
            {
                var response = _commandManager.HandleCommandFromServer(message);
                ChatHistory.Add(new ChatMessage(DateTime.Now, 0, author, response));
            }
            else
            {
                var msg = new ScriptedChatMsg { Author = author, Font = font, Target = playerId, Text = message };
                MyMultiplayerBase.SendScriptedChatMessage(ref msg);
                var character = MySession.Static.Players.TryGetIdentity(playerId)?.Character;
                var steamId = GetSteamId(playerId);
                if (character == null)
                    return;

                var addToGlobalHistoryMethod = typeof(MyCharacter).GetMethod("OnGlobalMessageSuccess", BindingFlags.Instance | BindingFlags.NonPublic);
                _networkManager.RaiseEvent(addToGlobalHistoryMethod, character, steamId, steamId, message);
            }
        }

        private void OnSessionLoaded()
        {
            Log.Info("Initializing Steam auth");
            MyMultiplayer.Static.ClientKicked += OnClientKicked;
            MyMultiplayer.Static.ClientLeft += OnClientLeft;

            //TODO: Move these with the methods?
            if (!RemoveHandlers())
            {
                Log.Error("Steam auth failed to initialize");
                return;
            }
            MyGameService.GameServer.ValidateAuthTicketResponse += ValidateAuthTicketResponse;
            MyGameService.GameServer.UserGroupStatusResponse += UserGroupStatusResponse;
            Log.Info("Steam auth initialized");
        }

        private void OnClientKicked(ulong steamId)
        {
            OnClientLeft(steamId, MyChatMemberStateChangeEnum.Kicked);
        }

        private void OnClientLeft(ulong steamId, MyChatMemberStateChangeEnum stateChange)
        {
            Players.TryGetValue(steamId, out PlayerViewModel vm);
            if (vm == null)
                vm = new PlayerViewModel(steamId);
            Log.Info($"{vm.Name} ({vm.SteamId}) {(ConnectionState)stateChange}.");
            PlayerLeft?.Invoke(vm);
            Players.Remove(steamId);
        }

        //TODO: Split the following into a new file?
        //These methods override some Keen code to allow us full control over client authentication.
        //This lets us have a server set to private (admins only) or friends (friends of all listed admins)
        [ReflectedGetter(Name = "m_members")]
        private static Func<MyDedicatedServerBase, List<ulong>> _members;
        [ReflectedGetter(Name = "m_waitingForGroup")]
        private static Func<MyDedicatedServerBase, HashSet<ulong>> _waitingForGroup;
        [ReflectedGetter(Name = "m_kickedClients")]
        private static Func<MyMultiplayerBase, Dictionary<ulong, int>> _kickedClients;
        //private HashSet<ulong> _waitingForFriends;
        private Dictionary<ulong, ulong> _gameOwnerIds = new Dictionary<ulong, ulong>();
        //private IMultiplayer _multiplayerImplementation;

        /// <summary>
        /// Removes Keen's hooks into some Steam events so we have full control over client authentication
        /// </summary>
        private static bool RemoveHandlers()
        {
            MethodInfo methodValidateAuthTicket = typeof(MyDedicatedServerBase).GetMethod("GameServer_ValidateAuthTicketResponse",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodValidateAuthTicket == null)
            {
                Log.Error("Unable to find the GameServer_ValidateAuthTicketResponse method to unhook");
                return false;
            }
            var eventValidateAuthTicket = Reflection.GetInstanceEvent(MyGameService.GameServer, nameof(MyGameService.GameServer.ValidateAuthTicketResponse))
                      .FirstOrDefault(x => x.Method == methodValidateAuthTicket) as Action<ulong, JoinResult, ulong>;
            if (eventValidateAuthTicket == null)
            {
                Log.Error(
                    "Unable to unhook the GameServer_ValidateAuthTicketResponse method from GameServer.ValidateAuthTicketResponse");
                Log.Debug("    Want to unhook {0}", methodValidateAuthTicket);
                Log.Debug("    Registered handlers: ");
                foreach (Delegate method in Reflection.GetInstanceEvent(MyGameService.GameServer,
                    nameof(MyGameService.GameServer.ValidateAuthTicketResponse)))
                    Log.Debug("       - " + method.Method);
                return false;
            }

            MethodInfo methodUserGroupStatus = typeof(MyDedicatedServerBase).GetMethod("GameServer_UserGroupStatus",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodUserGroupStatus == null)
            {
                Log.Error("Unable to find the GameServer_UserGroupStatus method to unhook");
                return false;
            }
            var eventUserGroupStatus = Reflection.GetInstanceEvent(MyGameService.GameServer, nameof(MyGameService.GameServer.UserGroupStatusResponse))
                      .FirstOrDefault(x => x.Method == methodUserGroupStatus)
                as Action<ulong, ulong, bool, bool>;
            if (eventUserGroupStatus == null)
            {
                Log.Error("Unable to unhook the GameServer_UserGroupStatus method from GameServer.UserGroupStatus");
                Log.Debug("    Want to unhook {0}", methodUserGroupStatus);
                Log.Debug("    Registered handlers: ");
                foreach (Delegate method in Reflection.GetInstanceEvent(MyGameService.GameServer, nameof(MyGameService.GameServer.UserGroupStatusResponse)))
                    Log.Debug("       - " + method.Method);
                return false;
            }

            MyGameService.GameServer.ValidateAuthTicketResponse -=
                eventValidateAuthTicket;
            MyGameService.GameServer.UserGroupStatusResponse -=
                eventUserGroupStatus;
            return true;
        }

        //Largely copied from SE
        private void ValidateAuthTicketResponse(ulong steamID, JoinResult response, ulong steamOwner)
        {
            Log.Debug($"ValidateAuthTicketResponse(user={steamID}, response={response}, owner={steamOwner}");
            if (IsClientBanned.Invoke(MyMultiplayer.Static, steamOwner) || MySandboxGame.ConfigDedicated.Banned.Contains(steamOwner))
            {
                UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.BannedByAdmins);
                RaiseClientKicked.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID);
            }
            else if (IsClientKicked.Invoke(MyMultiplayer.Static, steamOwner))
            {
                UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.KickedRecently);
                RaiseClientKicked.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID);
            }
            if (response != JoinResult.OK)
            {
                UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, response);
                return;
            }
            if (MyMultiplayer.Static.MemberLimit > 0 && _members.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Count - 1 >= MyMultiplayer.Static.MemberLimit)
            {
                UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.ServerFull);
                return;
            }
            if (MySandboxGame.ConfigDedicated.GroupID == 0uL ||
                MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) ||
                MySandboxGame.ConfigDedicated.Administrators.Contains(ConvertSteamIDFrom64(steamID)))
            {
                this.UserAccepted(steamID);
                return;
            }
            if (GetServerAccountType(MySandboxGame.ConfigDedicated.GroupID) != MyGameServiceAccountType.Clan)
            {
                UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.GroupIdInvalid);
                return;
            }
            if (MyGameService.GameServer.RequestGroupStatus(steamID, MySandboxGame.ConfigDedicated.GroupID))
            {
                _waitingForGroup.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Add(steamID);
                return;
            }
            UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.SteamServersOffline);
        }

        private void UserGroupStatusResponse(ulong userId, ulong groupId, bool member, bool officer)
        {
            if (groupId == MySandboxGame.ConfigDedicated.GroupID && _waitingForGroup.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Remove(userId))
            {
                if (member || officer)
                    UserAccepted(userId);
                else
                    UserRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, userId, JoinResult.NotInGroup);
            }
        }

        private void UserAccepted(ulong steamId)
        {
            UserAcceptedImpl.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamId);

            var vm = new PlayerViewModel(steamId) { State = ConnectionState.Connected };
            Log.Info($"Player {vm.Name} joined ({vm.SteamId})");
            Players.Add(steamId, vm);
            PlayerJoined?.Invoke(vm);
        }

        [ReflectedStaticMethod(Type = typeof(MyDedicatedServerBase))]
        private static Func<ulong, string> ConvertSteamIDFrom64;

        [ReflectedStaticMethod(Type = typeof(MyGameService))]
        private static Func<ulong, MyGameServiceAccountType> GetServerAccountType;

        [ReflectedMethod(Name = "UserAccepted")]
        private static Action<MyDedicatedServerBase, ulong> UserAcceptedImpl;

        [ReflectedMethod]
        private static Action<MyDedicatedServerBase, ulong, JoinResult> UserRejected;
        [ReflectedMethod]
        private static Func<MyMultiplayerBase, ulong, bool> IsClientBanned;
        [ReflectedMethod]
        private static Func<MyMultiplayerBase, ulong, bool> IsClientKicked;
        [ReflectedMethod]
        private static Action<MyMultiplayerBase, ulong> RaiseClientKicked;
    }
}
