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
        public static ServerManager Server;
        public static MultiplayerManager Multiplayer;
        public static PluginManager Plugins;

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
            }
        }

        public static void Reset()
        {
            Logger.Write("Resetting Piston");
            Server = null;
            Multiplayer = null;
            Plugins = null;
            _init = false;
        }
    }
}
