using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Sandbox;
using Sandbox.Engine.Platform;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.ModAPI;
using SpaceEngineers.Game;
using VRage.Steam;
using Torch.API;
using VRage.FileSystem;
using VRageRender;

namespace Torch.Client
{
    public class TorchClient : TorchBase, ITorchClient
    {
        private MyCommonProgramStartup _startup;
        private IMyRender _renderer;
        private const uint APP_ID = 244850;

        public override void Init()
        {
            Log.Info("Initializing Torch Client");
            base.Init();

            if (!File.Exists("steam_appid.txt"))
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(VRage.FastResourceLock).Assembly.Location) + "\\..");
            }

            SpaceEngineersGame.SetupBasicGameInfo();
            _startup = new MyCommonProgramStartup(RunArgs);
            if (_startup.PerformReporting())
                return;

            _startup.PerformAutoconnect();
            if (!_startup.CheckSingleInstance())
                return;

            var appDataPath = _startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(APP_ID, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath);
            MyInitializer.InitCheckSum();
            if (!_startup.Check64Bit())
                return;

            _startup.DetectSharpDxLeaksBeforeRun();
            using (var mySteamService = new SteamService(Game.IsDedicated, APP_ID))
            {
                _renderer = null;
                SpaceEngineersGame.SetupPerGameSettings();

                OverrideMenus();

                InitializeRender();

                if (!Game.IsDedicated)
                    MyFileSystem.InitUserSpecific(mySteamService.UserId.ToString());
            }

            _startup.DetectSharpDxLeaksAfterRun();
            MyInitializer.InvokeAfterRun();
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
                spaceEngineersGame.OnGameLoaded += SpaceEngineersGame_OnGameLoaded;
                spaceEngineersGame.Run();
            }
        }

        private void SpaceEngineersGame_OnGameLoaded(object sender, EventArgs e)
        {
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
