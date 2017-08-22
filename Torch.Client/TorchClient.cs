using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform;
using Sandbox.Game;
using SpaceEngineers.Game;
using VRage.Steam;
using Torch.API;
using VRage;
using VRage.FileSystem;
using VRage.GameServices;
using VRageRender;
using VRageRender.ExternalApp;

namespace Torch.Client
{
    public class TorchClient : TorchBase, ITorchClient
    {
        private MyCommonProgramStartup _startup;
        private IMyRender _renderer;
        private const uint APP_ID = 244850;

        public TorchClient()
        {
            Config = new TorchClientConfig();
        }

        public override void Init()
        {
            Directory.SetCurrentDirectory(Program.SpaceEngineersInstallAlias);
            MyFileSystem.ExePath = Path.Combine(Program.SpaceEngineersInstallAlias, Program.SpaceEngineersBinaries);
            Log.Info("Initializing Torch Client");
            base.Init();
            
            SpaceEngineersGame.SetupBasicGameInfo();
            _startup = new MyCommonProgramStartup(RunArgs);
            if (_startup.PerformReporting())
                throw new InvalidOperationException("Torch client won't launch when started in error reporting mode");

            _startup.PerformAutoconnect();
            if (!_startup.CheckSingleInstance())
                throw new InvalidOperationException("Only one instance of Space Engineers can be running at a time.");

            var appDataPath = _startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(APP_ID, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath);
            MyInitializer.InitCheckSum();
            _startup.InitSplashScreen();
            if (!_startup.Check64Bit())
                throw new InvalidOperationException("Torch requires a 64bit operating system");

            _startup.DetectSharpDxLeaksBeforeRun();
            var steamService = new SteamService(Game.IsDedicated, APP_ID);
            MyServiceManager.Instance.AddService<IMyGameService>(steamService);
            _renderer = null;
            SpaceEngineersGame.SetupPerGameSettings();
            // I'm sorry, but it's what Keen does in SpaceEngineers.MyProgram
#pragma warning disable 612
            SpaceEngineersGame.SetupRender();
#pragma warning restore 612
            InitializeRender();
            if (!_startup.CheckSteamRunning())
                throw new InvalidOperationException("Space Engineers requires steam to be running");

            if (!Game.IsDedicated)
                MyFileSystem.InitUserSpecific(MyGameService.UserId.ToString());
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
        
        public override void Start()
        {
            using (var spaceEngineersGame = new SpaceEngineersGame(RunArgs))
            {
                Log.Info("Starting client");
                OverrideMenus();
                spaceEngineersGame.OnGameLoaded += SpaceEngineersGame_OnGameLoaded;
                spaceEngineersGame.OnGameExit += Dispose;
                spaceEngineersGame.Run(false, _startup.DisposeSplashScreen);
            }
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
            var window = renderWindowField.GetValue(MySandboxGame.Static.GameRenderComponent.RenderThread) as System.Windows.Forms.Form;
            if (window != null)
                renderThread.Invoke(() =>
                {
                    window.Text = title;
                });
        }

        private void SpaceEngineersGame_OnGameLoaded(object sender, EventArgs e)
        {
            SetRenderWindowTitle($"Space Engineers v{GameVersion} with Torch v{TorchVersion}");
        }

        public override void Dispose()
        {
            MyGameService.ShutDown();
            _startup.DetectSharpDxLeaksAfterRun();
            MyInitializer.InvokeAfterRun();
        }

        public override void Stop()
        {
            MySandboxGame.ExitThreadSafe();
        }

        private void InitializeRender()
        {
            try
            {
                if (Game.IsDedicated)
                {
                    _renderer = new MyNullRender();
                }
                else
                {
                    var graphicsRenderer = MySandboxGame.Config.GraphicsRenderer;
                    if (graphicsRenderer == MySandboxGame.DirectX11RendererKey)
                    {
                        _renderer = new MyDX11Render();
                        if (!_renderer.IsSupported)
                        {
                            MySandboxGame.Log.WriteLine("DirectX 11 renderer not supported. No renderer to revert back to.");
                            _renderer = null;
                        }
                    }
                    if (_renderer == null)
                        throw new MyRenderException("The current version of the game requires a Dx11 card. \\n For more information please see : http://blog.marekrosa.org/2016/02/space-engineers-news-full-source-code_26.html", MyRenderExceptionEnum.GpuNotSupported);

                    MySandboxGame.Config.GraphicsRenderer = graphicsRenderer;
                }

                MyRenderProxy.Initialize(_renderer);
                MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
                MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Render Initialization Failed");
                Environment.Exit(-1);
            }
        }
    }
}
