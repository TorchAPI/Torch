using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SteamSDK;
using VRage.Steam;
using Sandbox;
using Sandbox.Engine.Networking;
using Torch.Utils;
using VRage.GameServices;

namespace Torch
{
    /// <summary>
    /// SNAGGED FROM PHOENIX84'S SE WORKSHOP TOOL
    /// Keen's steam service calls RestartIfNecessary, which triggers steam to think the game was launched
    /// outside of Steam, which causes this process to exit, and the game to launch instead with an arguments warning.
    /// We have to override the default behavior, then forcibly set the correct options.
    /// </summary>
    public class SteamService : MySteamService
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
        [ReflectedSetter(Name = nameof(SteamServerAPI))]
        private static Action<MySteamService, SteamServerAPI> _steamServerAPISetter;
        [ReflectedSetter(Name = "m_gameServer")]
        private static Action<MySteamService, MySteamGameServer> _steamGameServerSetter;
        [ReflectedSetter(Name = nameof(AppId))]
        private static Action<MySteamService, uint> _steamAppIdSetter;
        [ReflectedSetter(Name = nameof(API))]
        private static Action<MySteamService, SteamAPI> _steamApiSetter;
        [ReflectedSetter(Name = nameof(IsActive))]
        private static Action<MySteamService, bool> _steamIsActiveSetter;
        [ReflectedSetter(Name = nameof(UserId))]
        private static Action<MySteamService, ulong> _steamUserIdSetter;
        [ReflectedSetter(Name = nameof(UserName))]
        private static Action<MySteamService, string> _steamUserNameSetter;
        [ReflectedSetter(Name = nameof(OwnsGame))]
        private static Action<MySteamService, bool> _steamOwnsGameSetter;
        [ReflectedSetter(Name = nameof(UserUniverse))]
        private static Action<MySteamService, MyGameServiceUniverse> _steamUserUniverseSetter;
        [ReflectedSetter(Name = nameof(BranchName))]
        private static Action<MySteamService, string> _steamBranchNameSetter;
        [ReflectedSetter(Name = nameof(InventoryAPI))]
        private static Action<MySteamService, MySteamInventory> _steamInventoryAPISetter;
        [ReflectedMethod]
        private static Action<MySteamService> RegisterCallbacks;
        [ReflectedSetter(Name = nameof(Peer2Peer))]
        private static Action<MySteamService, IMyPeer2Peer> _steamPeer2PeerSetter;
#pragma warning restore 649

        public SteamService(bool isDedicated, uint appId)
            : base(true, appId)
        {
            SteamServerAPI.Instance.Dispose();
            _steamServerAPISetter.Invoke(this, null);
            _steamGameServerSetter.Invoke(this, null);
            _steamAppIdSetter.Invoke(this, appId);

            if (isDedicated)
            {
                _steamServerAPISetter.Invoke(this, null);
                _steamGameServerSetter.Invoke(this, new MySteamGameServer());
            }
            else
            {
                SteamAPI steamApi = SteamAPI.Instance;
                _steamApiSetter.Invoke(this, steamApi);
                bool initResult = steamApi.Init();
                if (!initResult)
                    _log.Warn("Failed to initialize SteamService");
                _steamIsActiveSetter.Invoke(this, initResult);

                if (IsActive)
                {
                    _steamUserIdSetter.Invoke(this, steamApi.GetSteamUserId());
                    _steamUserNameSetter.Invoke(this, steamApi.GetSteamName());
                    _steamOwnsGameSetter.Invoke(this, steamApi.HasGame());
                    _steamUserUniverseSetter.Invoke(this, (MyGameServiceUniverse)steamApi.GetSteamUserUniverse());
                    _steamBranchNameSetter.Invoke(this, steamApi.GetBranchName());
                    steamApi.LoadStats();

                    _steamInventoryAPISetter.Invoke(this, new MySteamInventory());
                    RegisterCallbacks(this);
                } else
                    _log.Warn("SteamService isn't initialized; Torch Client won't start");
            }

            _steamPeer2PeerSetter.Invoke(this, new MySteamPeer2Peer());
        }
    }
}
