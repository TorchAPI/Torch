using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Steamworks;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Utils;
using Torch.ViewModels;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRage.Network;
using VRage.Steam;

namespace Torch.Server.Managers
{
    public class MultiplayerManagerDedicated : MultiplayerManagerBase, IMultiplayerManagerServer
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
        [ReflectedGetter(Name = "m_members")]
        private static Func<MyDedicatedServerBase, List<ulong>> _members;

        [ReflectedGetter(Name = "m_waitingForGroup")]
        private static Func<MyDedicatedServerBase, HashSet<ulong>> _waitingForGroup;
#pragma warning restore 649

        /// <inheritdoc />
        public IReadOnlyList<ulong> BannedPlayers => MySandboxGame.ConfigDedicated.Banned;

        private Dictionary<ulong, ulong> _gameOwnerIds = new Dictionary<ulong, ulong>();

        [Dependency]
        private InstanceManager _instanceManager;

        /// <inheritdoc />
        public MultiplayerManagerDedicated(ITorchBase torch) : base(torch)
        {
        }

        /// <inheritdoc />
        public void KickPlayer(ulong steamId) => Torch.Invoke(() => MyMultiplayer.Static.KickClient(steamId));

        /// <inheritdoc />
        public void BanPlayer(ulong steamId, bool banned = true)
        {
            Torch.Invoke(() =>
            {
                MyMultiplayer.Static.BanClient(steamId, banned);
            });
        }

        internal void RaiseClientBanned(ulong user, bool banned)
        {
            PlayerBanned?.Invoke(user, banned);
            Torch.Invoke(() =>
                         {
                             if(_gameOwnerIds.TryGetValue(user, out ulong owner))
                                 MyMultiplayer.Static.BanClient(owner, banned);
                         });
        }

        internal void RaiseClientKicked(ulong user)
        {
            PlayerKicked?.Invoke(user);
        }

        /// <inheritdoc />
        public bool IsBanned(ulong steamId) => _isClientBanned.Invoke(MyMultiplayer.Static, steamId) ||
                                               MySandboxGame.ConfigDedicated.Banned.Contains(steamId);

        /// <inheritdoc />
        public event Action<ulong> PlayerKicked;

        /// <inheritdoc />
        public event Action<ulong, bool> PlayerBanned;

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            _gameServerValidateAuthTicketReplacer = _gameServerValidateAuthTicketFactory.Invoke();
            _gameServerUserGroupStatusReplacer = _gameServerUserGroupStatusFactory.Invoke();
            _gameServerValidateAuthTicketReplacer.Replace(
                new Action<ulong, JoinResult, ulong>(ValidateAuthTicketResponse), MyGameService.GameServer);
            _gameServerUserGroupStatusReplacer.Replace(new Action<ulong, ulong, bool, bool>(UserGroupStatusResponse),
                MyGameService.GameServer);
            _log.Info("Inserted steam authentication intercept");
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            if (_gameServerValidateAuthTicketReplacer != null && _gameServerValidateAuthTicketReplacer.Replaced)
                _gameServerValidateAuthTicketReplacer.Restore(MyGameService.GameServer);
            if (_gameServerUserGroupStatusReplacer != null && _gameServerUserGroupStatusReplacer.Replaced)
                _gameServerUserGroupStatusReplacer.Restore(MyGameService.GameServer);
            _log.Info("Removed steam authentication intercept");
            base.Detach();
        }


#pragma warning disable 649
        [ReflectedEventReplace(typeof(MySteamGameServer), nameof(MySteamGameServer.ValidateAuthTicketResponse),
            typeof(MyDedicatedServerBase), "GameServer_ValidateAuthTicketResponse")]
        private static Func<ReflectedEventReplacer> _gameServerValidateAuthTicketFactory;

        [ReflectedEventReplace(typeof(MySteamGameServer), nameof(MySteamGameServer.UserGroupStatusResponse),
            typeof(MyDedicatedServerBase), "GameServer_UserGroupStatus")]
        private static Func<ReflectedEventReplacer> _gameServerUserGroupStatusFactory;

        private ReflectedEventReplacer _gameServerValidateAuthTicketReplacer;
        private ReflectedEventReplacer _gameServerUserGroupStatusReplacer;
#pragma warning restore 649

        #region CustomAuth

#pragma warning disable 649
        [ReflectedStaticMethod(Type = typeof(MyDedicatedServerBase), Name = "ConvertSteamIDFrom64")]
        private static Func<ulong, string> _convertSteamIDFrom64;

        [ReflectedStaticMethod(Type = typeof(MyGameService), Name = "GetServerAccountType")]
        private static Func<ulong, MyGameServiceAccountType> _getServerAccountType;

        [ReflectedMethod(Name = "UserAccepted")]
        private static Action<MyDedicatedServerBase, ulong> _userAcceptedImpl;

        [ReflectedMethod(Name = "UserRejected")]
        private static Action<MyDedicatedServerBase, ulong, JoinResult> _userRejected;

        [ReflectedMethod(Name = "IsClientBanned")]
        private static Func<MyMultiplayerBase, ulong, bool> _isClientBanned;

        [ReflectedMethod(Name = "IsClientKicked")]
        private static Func<MyMultiplayerBase, ulong, bool> _isClientKicked;

        [ReflectedMethod(Name = "RaiseClientKicked")]
        private static Action<MyMultiplayerBase, ulong> _raiseClientKicked;
#pragma warning restore 649

        private const int _waitListSize = 32;
        private readonly List<WaitingForGroup> _waitingForGroupLocal = new List<WaitingForGroup>(_waitListSize);

        private struct WaitingForGroup
        {
            public readonly ulong SteamId;
            public readonly JoinResult Response;
            public readonly ulong SteamOwner;

            public WaitingForGroup(ulong id, JoinResult response, ulong owner)
            {
                SteamId = id;
                Response = response;
                SteamOwner = owner;
            }
        }

        //Largely copied from SE
        private void ValidateAuthTicketResponse(ulong steamId, JoinResult response, ulong steamOwner)
        {
            //SteamNetworking.GetP2PSessionState(new CSteamID(steamId), out P2PSessionState_t state);            
            //state.GetRemoteIP();
            MyP2PSessionState statehack = new MyP2PSessionState();
            VRage.Steam.MySteamService.Static.Peer2Peer.GetSessionState(steamId, ref statehack);
            var ip = new IPAddress(BitConverter.GetBytes(statehack.RemoteIP).Reverse().ToArray());

            Torch.CurrentSession.KeenSession.PromotedUsers.TryGetValue(steamId, out MyPromoteLevel promoteLevel);

            _log.Debug($"ValidateAuthTicketResponse(user={steamId}, response={response}, owner={steamOwner}, permissions={promoteLevel})");

            _log.Info($"Connection attempt by {steamId} from {ip}");
            // TODO implement IP bans
            var config = (TorchConfig) Torch.Config;
            if (config.EnableWhitelist && !config.Whitelist.Contains(steamId))
            {
                _log.Warn($"Rejecting user {steamId} because they are not whitelisted in Torch.cfg.");
                UserRejected(steamId, JoinResult.NotInGroup);
            }
            else if(config.EnableReservedSlots && config.ReservedPlayers.Contains(steamId))
                UserAccepted(steamId);
            else if (Torch.CurrentSession.KeenSession.OnlineMode == MyOnlineModeEnum.OFFLINE &&
                     promoteLevel < MyPromoteLevel.Admin)
            {
                _log.Warn($"Rejecting user {steamId}, world is set to offline and user is not admin.");
                UserRejected(steamId, JoinResult.TicketCanceled);
            }
            else if (MySandboxGame.ConfigDedicated.GroupID == 0uL)
                RunEvent(new ValidateAuthTicketEvent(steamId, steamOwner, response, 0, true, false));
            else if (_getServerAccountType(MySandboxGame.ConfigDedicated.GroupID) != MyGameServiceAccountType.Clan)
                UserRejected(steamId, JoinResult.GroupIdInvalid);
            else if (MyGameService.GameServer.RequestGroupStatus(steamId, MySandboxGame.ConfigDedicated.GroupID))
                lock (_waitingForGroupLocal)
                {
                    if (_waitingForGroupLocal.Count >= _waitListSize)
                        _waitingForGroupLocal.RemoveAt(0);
                    _waitingForGroupLocal.Add(new WaitingForGroup(steamId, response, steamOwner));
                }
            else
                UserRejected(steamId, JoinResult.SteamServersOffline);
        }

        private void RunEvent(ValidateAuthTicketEvent info)
        {
            MultiplayerManagerDedicatedEventShim.RaiseValidateAuthTicket(ref info);

            if (info.FutureVerdict == null)
            {
                if (IsBanned(info.SteamOwner) || IsBanned(info.SteamID))
                    CommitVerdict(info.SteamID, JoinResult.BannedByAdmins);
                else if (_isClientKicked(MyMultiplayer.Static, info.SteamID) ||
                         _isClientKicked(MyMultiplayer.Static, info.SteamOwner))
                    CommitVerdict(info.SteamID, JoinResult.KickedRecently);
                else if (info.SteamResponse == JoinResult.OK)
                {
                    //Admins can bypass member limit
                    if (MySandboxGame.ConfigDedicated.Administrators.Contains(info.SteamID.ToString()) ||
                        MySandboxGame.ConfigDedicated.Administrators.Contains(_convertSteamIDFrom64(info.SteamID)))
                        CommitVerdict(info.SteamID, JoinResult.OK);
                    //Server counts as a client, so subtract 1 from MemberCount
                    else if (MyMultiplayer.Static.MemberLimit > 0 &&
                             MyMultiplayer.Static.MemberCount - 1 >= MyMultiplayer.Static.MemberLimit)
                        CommitVerdict(info.SteamID, JoinResult.ServerFull);
                    else if (MySandboxGame.ConfigDedicated.GroupID == 0uL)
                        CommitVerdict(info.SteamID, JoinResult.OK);
                    else
                    {
                        if (MySandboxGame.ConfigDedicated.GroupID == info.Group && (info.Member || info.Officer))
                            CommitVerdict(info.SteamID, JoinResult.OK);
                        else
                            CommitVerdict(info.SteamID, JoinResult.NotInGroup);
                    }
                }
                else
                    CommitVerdict(info.SteamID, info.SteamResponse);
                
                return;
            }

            info.FutureVerdict.ContinueWith((task) =>
            {
                JoinResult verdict;
                if (task.IsFaulted)
                {
                    _log.Error(task.Exception, $"Future validation verdict faulted");
                    verdict = JoinResult.TicketCanceled;
                }
                else
                    verdict = task.Result;

                Torch.Invoke(() => { CommitVerdict(info.SteamID, verdict); });
            });
        }

        private void CommitVerdict(ulong steamId, JoinResult verdict)
        {
            if (verdict == JoinResult.OK)
                UserAccepted(steamId);
            else
                UserRejected(steamId, verdict);
        }

        private void UserGroupStatusResponse(ulong userId, ulong groupId, bool member, bool officer)
        {
            lock (_waitingForGroupLocal)
                for (var j = 0; j < _waitingForGroupLocal.Count; j++)
                {
                    var wait = _waitingForGroupLocal[j];
                    if (wait.SteamId == userId)
                    {
                        RunEvent(new ValidateAuthTicketEvent(wait.SteamId, wait.SteamOwner, wait.Response, groupId,
                            member, officer));
                        _waitingForGroupLocal.RemoveAt(j);
                        break;
                    }
                }
        }

        private void UserRejected(ulong steamId, JoinResult reason)
        {
            _userRejected.Invoke((MyDedicatedServerBase) MyMultiplayer.Static, steamId, reason);
        }

        private void UserAccepted(ulong steamId)
        {
            _userAcceptedImpl.Invoke((MyDedicatedServerBase) MyMultiplayer.Static, steamId);
            base.RaiseClientJoined(steamId);
        }

        #endregion
    }
}