using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Sandbox;
using Sandbox.Engine.Platform;
using Sandbox.Game;
using SpaceEngineers.Game;
using VRage.FileSystem;
using VRageRender;
using Piston;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using VRageMath;

namespace Piston.Client
{
    /// <summary>
    ///     This is nearly all Keen's code.
    /// </summary>
    public static class MyProgram
    {
        private static MyCommonProgramStartup _startup;
        private static IMyRender _renderer;
        private static readonly uint AppId = 244850u;
        private static PluginManager _pluginManager;

        private static void Main(string[] args)
        {
            if (!File.Exists("steam_appid.txt"))
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(VRage.FastResourceLock).Assembly.Location) + "\\..");
            }

            //Add myself to the credits because I'm awesome.
            var credits = new MyCreditsDepartment("Piston Developed By") {Persons = new List<MyCreditsPerson> {new MyCreditsPerson("JIMMACLE")}};
            MyPerGameSettings.Credits.Departments.Add(credits);

            SpaceEngineersGame.SetupBasicGameInfo();
            _startup = new MyCommonProgramStartup(args);
            if (_startup.PerformReporting())
                return;

            _startup.PerformAutoconnect();
            if (!_startup.CheckSingleInstance())
                return;

            var appDataPath = _startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath, false);
            MyInitializer.InitCheckSum();
            if (!_startup.Check64Bit())
                return;

            _startup.DetectSharpDxLeaksBeforeRun();
            using (var mySteamService = new SteamService(Game.IsDedicated, AppId))
            {
                _renderer = null;
                SpaceEngineersGame.SetupPerGameSettings();
                SpaceEngineersGame.SetupRender();
                try
                {
                    InitializeRender();
                }
                catch (MyRenderException ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                var services = new VRageGameServices(mySteamService);
                if (!Game.IsDedicated)
                    MyFileSystem.InitUserSpecific(mySteamService.UserId.ToString());
                using (var spaceEngineersGame = new SpaceEngineersGame(services, args))
                {
                    Logger.Write("Starting SE...");
                    spaceEngineersGame.OnGameLoaded += SpaceEngineersGame_OnGameLoaded;
                    MyGuiSandbox.GuiControlCreated += GuiControlCreated;
                    spaceEngineersGame.Run();
                }
            }

            _startup.DetectSharpDxLeaksAfterRun();
            MyInitializer.InvokeAfterRun();
        }

        private static void GuiControlCreated(object o)
        {
            var menu = o as MyGuiScreenMainMenu;
            if (menu != null)
            {
                Logger.Write("Adding button to main menu");
                menu.Controls.Add(new MyGuiControlImageButton("PistonButton", text: new StringBuilder("Piston"), size: new Vector2(20), position: new Vector2(0)));
            }
        }

        private static void SpaceEngineersGame_OnGameLoaded(object sender, System.EventArgs e)
        {
            _pluginManager = new PluginManager();
            _pluginManager.LoadAllPlugins();

            //Fix Marek's name.
            MyPerGameSettings.Credits.Departments[1].Persons[0].Name.Clear().Append("MEARK ROAS");
        }

        private static void InitializeRender()
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
    }
}