using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Sandbox;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageRender;
using Game = Sandbox.Engine.Platform.Game;

namespace Piston.Client
{
    public class GameInitializer
    {
        private MyCommonProgramStartup _startup;
        private IMyRender _renderer;
        private const uint APP_ID = 244850;
        private PluginManager _pluginManager;
        private readonly string[] _args;
        private VRageGameServices _services;

        public GameInitializer(string[] args)
        {
            _args = args;
        }

        public void TryInit()
        {
            if (!File.Exists("steam_appid.txt"))
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(VRage.FastResourceLock).Assembly.Location) + "\\..");
            }

            //Add myself to the credits because I'm awesome.
            var credits = new MyCreditsDepartment("Piston Developed By") { Persons = new List<MyCreditsPerson>
            {
                new MyCreditsPerson("JIMMACLE"),
                new MyCreditsPerson("REXXAR"),
                new MyCreditsPerson("PHOENIXTHESAGE")
            } };
            MyPerGameSettings.Credits.Departments.Add(credits);

            SpaceEngineersGame.SetupBasicGameInfo();
            _startup = new MyCommonProgramStartup(_args);
            if (_startup.PerformReporting())
                return;

            _startup.PerformAutoconnect();
            if (!_startup.CheckSingleInstance())
                return;

            var appDataPath = _startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(APP_ID, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath, false);
            MyInitializer.InitCheckSum();
            if (!_startup.Check64Bit())
                return;

            _startup.DetectSharpDxLeaksBeforeRun();
            using (var mySteamService = new SteamService(Game.IsDedicated, APP_ID))
            {
                _renderer = null;
                SpaceEngineersGame.SetupPerGameSettings();

                InitializeRender();

                _services = new VRageGameServices(mySteamService);
                if (!Game.IsDedicated)
                    MyFileSystem.InitUserSpecific(mySteamService.UserId.ToString());
            }

            _startup.DetectSharpDxLeaksAfterRun();
            MyInitializer.InvokeAfterRun();
        }

        public void RunGame()
        {
            using (var spaceEngineersGame = new SpaceEngineersGame(_services, _args))
            {
                Logger.Write("Starting SE...");
                spaceEngineersGame.OnGameLoaded += SpaceEngineersGame_OnGameLoaded;
                MyGuiSandbox.GuiControlCreated += GuiControlCreated;
                spaceEngineersGame.Run();
            }
        }

        private void GuiControlCreated(object o)
        {
            var menu = o as MyGuiScreenMainMenu;
            if (menu != null)
            {
                var pistonBtn = new MyGuiControlImageButton
                {
                    Name = "PistonButton",
                    Text = "Piston",
                    HighlightType = MyGuiControlHighlightType.WHEN_CURSOR_OVER,
                    Visible = true,
                };

                menu.Controls.Add(pistonBtn);
            }
        }

        private void SpaceEngineersGame_OnGameLoaded(object sender, EventArgs e)
        {
            _pluginManager = new PluginManager();
            _pluginManager.LoadAllPlugins();

            //Fix Marek's name.
            MyPerGameSettings.Credits.Departments[1].Persons[0].Name.Clear().Append("MEARK ROAS");
            MyScreenManager.AddScreen(new PistonConsoleScreen());
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
                MyRenderProxy.IS_OFFICIAL = true;
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
