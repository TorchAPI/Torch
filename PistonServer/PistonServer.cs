using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Piston;

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
            if (!_init)
            {
                Logger.Write("Initializing Piston");
                _init = true;
                Server = new ServerManager();
                Multiplayer = new MultiplayerManager(Server);
                Plugins = new PluginManager();
                UI = new PistonUI();
            }
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
