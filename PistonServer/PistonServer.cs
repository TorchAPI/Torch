using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Piston;
using Piston.Server.Properties;
using Sandbox;
using Sandbox.Game;
using SpaceEngineers.Game;
using VRage.Dedicated;
using VRage.Game;
using VRage.Game.SessionComponents;

namespace Piston.Server
{
    /// <summary>
    /// Entry point for all Piston server functionality.
    /// </summary>
    public static class PistonServer
    {
        public static ServerManager Server { get; private set; }
        public static MultiplayerManager Multiplayer { get; private set; }
        public static PluginManager Plugins { get; private set; }
        public static PistonUI UI { get; private set; }

        private static bool _init;

        public static void Init()
        {
            if (_init)
                return;

            Logger.Write("Initializing Piston");
            _init = true;
            Server = new ServerManager();
            Multiplayer = new MultiplayerManager(Server);
            Plugins = new PluginManager();
            UI = new PistonUI();

            Server.SessionLoaded += Plugins.LoadAllPlugins;
            Server.InitSandbox();
            SteamHelper.Init();
            UI.PropGrid.SetObject(MySandboxGame.ConfigDedicated);
        }

        public static void Reset()
        {
            Logger.Write("Resetting Piston");
            Server.Dispose();
            UI.Close();

            Server = null;
            Multiplayer = null;
            Plugins = null;
            UI = null;
            _init = false;
        }
    }
}
