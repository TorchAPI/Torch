using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform;
using Sandbox.Game;
using SpaceEngineers.Game;
using VRage.Steam;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Client.Manager;
using Torch.Client.UI;
using Torch.Session;
using VRage;
using VRage.FileSystem;
using VRage.GameServices;
using VRageRender;
using VRageRender.ExternalApp;

namespace Torch.Client
{
    public class TorchClient : TorchBase, ITorchClient
    {
        protected override uint SteamAppId => 244850;
        protected override string SteamAppName => "SpaceEngineers";

        public TorchClient()
        {
            Config = new TorchClientConfig();
            var sessionManager = Managers.GetManager<ITorchSessionManager>();
            sessionManager.AddFactory((x) => MyMultiplayer.Static is MyMultiplayerLobby
                ? new MultiplayerManagerLobby(this)
                : null);
            sessionManager.AddFactory((x) => MyMultiplayer.Static is MyMultiplayerClientBase
                ? new MultiplayerManagerClient(this)
                : null);
        }

        public override void Init()
        {
            Directory.SetCurrentDirectory(Program.SpaceEngineersInstallAlias);
            MyFileSystem.ExePath = Path.Combine(Program.SpaceEngineersInstallAlias, Program.SpaceEngineersBinaries);
            Log.Info("Initializing Torch Client");
            Config.InstancePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                SteamAppName);
            base.Init();
            OverrideMenus();
            SetRenderWindowTitle($"Space Engineers v{GameVersion} with Torch v{TorchVersion}");
        }

        private void OverrideMenus()
        {
            var credits = new MyCreditsDepartment("Torch Developed By")
            {
                Persons = new List<MyCreditsPerson>
                {
                    new MyCreditsPerson("THE TORCH TEAM"),
                    new MyCreditsPerson("http://github.com/TorchSE"),
                }
            };
            MyPerGameSettings.Credits.Departments.Insert(0, credits);

            MyPerGameSettings.GUI.MainMenu = typeof(TorchMainMenuScreen);
        }

        private void SetRenderWindowTitle(string title)
        {
            MyRenderThread renderThread = MySandboxGame.Static?.GameRenderComponent?.RenderThread;
            if (renderThread == null)
                return;
            FieldInfo renderWindowField = typeof(MyRenderThread).GetField("m_renderWindow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (renderWindowField == null)
                return;
            var window =
                renderWindowField.GetValue(MySandboxGame.Static.GameRenderComponent.RenderThread) as
                    System.Windows.Forms.Form;
            if (window != null)
                renderThread.Invoke(() => { window.Text = title; });
        }

        public override void Restart()
        {
            throw new NotImplementedException();
        }
    }
}