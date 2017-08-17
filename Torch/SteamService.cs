using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamSDK;
using VRage.Steam;
using Sandbox;

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
        public SteamService(bool isDedicated, uint appId)
            : base(true, appId)
        {
            // TODO: Add protection for this mess... somewhere
            SteamSDK.SteamServerAPI.Instance.Dispose();
            var steam = typeof(MySteamService);

            steam.GetProperty("SteamServerAPI").GetSetMethod(true).Invoke(this, new object[] { null });
            steam.GetField("m_gameServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, null);

            steam.GetProperty("AppId").GetSetMethod(true).Invoke(this, new object[] { appId });
            if (isDedicated)
            {
                steam.GetProperty("SteamServerAPI").GetSetMethod(true).Invoke(this, new object[] { null });
                steam.GetField("m_gameServer").SetValue(this, new MySteamGameServer());
            }
            else
            {
                var SteamAPI = SteamSDK.SteamAPI.Instance;
                steam.GetProperty("API").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.Instance });
                steam.GetProperty("IsActive").GetSetMethod(true).Invoke(this, new object[] { 
                    SteamAPI.Instance.Init()
                 });

                if (IsActive)
                {
                    steam.GetProperty("UserId").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamUserId() });
                    steam.GetProperty("UserName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamName() });
                    steam.GetProperty("OwnsGame").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.HasGame() });
                    steam.GetProperty("UserUniverse").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamUserUniverse() });
                    steam.GetProperty("BranchName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetBranchName() });
                    SteamAPI.LoadStats();

                    steam.GetProperty("InventoryAPI").GetSetMethod(true).Invoke(this, new object[] { new MySteamInventory() });

                    steam.GetMethod("RegisterCallbacks",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(this, null);
                }
            }

            steam.GetProperty("Peer2Peer").GetSetMethod(true).Invoke(this, new object[] { new MySteamPeer2Peer() });
        }
    }
}
