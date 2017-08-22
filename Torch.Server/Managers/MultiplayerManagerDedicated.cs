using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Utils;
using Torch.ViewModels;
using VRage.GameServices;
using VRage.Network;

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

        private Dictionary<ulong, ulong> _gameOwnerIds = new Dictionary<ulong, ulong>();

        /// <inheritdoc />
        public MultiplayerManagerDedicated(ITorchBase torch) : base(torch) { }

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

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            _gameServerValidateAuthTicketReplacer = _gameServerValidateAuthTicketFactory.Invoke();
            _gameServerUserGroupStatusReplacer = _gameServerUserGroupStatusFactory.Invoke();
            _gameServerValidateAuthTicketReplacer.Replace(new Action<ulong, JoinResult, ulong>(ValidateAuthTicketResponse), MyGameService.GameServer);
            _gameServerUserGroupStatusReplacer.Replace(new Action<ulong, ulong, bool, bool>(UserGroupStatusResponse), MyGameService.GameServer);
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
        [ReflectedEventReplace(typeof(IMyGameServer), nameof(IMyGameServer.ValidateAuthTicketResponse), typeof(MyDedicatedServerBase), "GameServer_ValidateAuthTicketResponse")]
        private static Func<ReflectedEventReplacer> _gameServerValidateAuthTicketFactory;
        [ReflectedEventReplace(typeof(IMyGameServer), nameof(IMyGameServer.UserGroupStatusResponse), typeof(MyDedicatedServerBase), "GameServer_UserGroupStatus")]
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

        //Largely copied from SE
        private void ValidateAuthTicketResponse(ulong steamID, JoinResult response, ulong steamOwner)
        {
            _log.Debug($"ValidateAuthTicketResponse(user={steamID}, response={response}, owner={steamOwner}");
            if (_isClientBanned.Invoke(MyMultiplayer.Static, steamOwner) || MySandboxGame.ConfigDedicated.Banned.Contains(steamOwner))
            {
                _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.BannedByAdmins);
                _raiseClientKicked.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID);
            }
            else if (_isClientKicked.Invoke(MyMultiplayer.Static, steamOwner))
            {
                _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.KickedRecently);
                _raiseClientKicked.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID);
            }
            if (response != JoinResult.OK)
            {
                _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, response);
                return;
            }
            if (MyMultiplayer.Static.MemberLimit > 0 && _members.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Count - 1 >= MyMultiplayer.Static.MemberLimit)
            {
                _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.ServerFull);
                return;
            }
            if (MySandboxGame.ConfigDedicated.GroupID == 0uL ||
                MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) ||
                MySandboxGame.ConfigDedicated.Administrators.Contains(_convertSteamIDFrom64(steamID)))
            {
                this.UserAccepted(steamID);
                return;
            }
            if (_getServerAccountType(MySandboxGame.ConfigDedicated.GroupID) != MyGameServiceAccountType.Clan)
            {
                _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.GroupIdInvalid);
                return;
            }
            if (MyGameService.GameServer.RequestGroupStatus(steamID, MySandboxGame.ConfigDedicated.GroupID))
            {
                _waitingForGroup.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Add(steamID);
                return;
            }
            _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamID, JoinResult.SteamServersOffline);
        }

        private void UserGroupStatusResponse(ulong userId, ulong groupId, bool member, bool officer)
        {
            if (groupId == MySandboxGame.ConfigDedicated.GroupID && _waitingForGroup.Invoke((MyDedicatedServerBase)MyMultiplayer.Static).Remove(userId))
            {
                if (member || officer)
                    UserAccepted(userId);
                else
                    _userRejected.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, userId, JoinResult.NotInGroup);
            }
        }
        private void UserAccepted(ulong steamId)
        {
            _userAcceptedImpl.Invoke((MyDedicatedServerBase)MyMultiplayer.Static, steamId);
            base.RaiseClientJoined(steamId);
        }
        #endregion
    }
}
