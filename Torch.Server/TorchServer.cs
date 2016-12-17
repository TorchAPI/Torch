using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Sandbox;
using Sandbox.Game;
using SpaceEngineers.Game;
using Torch.API;
using Torch.Launcher;
using VRage.Dedicated;
using VRage.Game;
using VRage.Game.SessionComponents;

namespace Torch.Server
{
    /// <summary>
    /// Entry point for all Piston server functionality.
    /// </summary>
    public class TorchServer : ITorchServer
    {
        public ServerManager Server { get; private set; }
        public MultiplayerManager Multiplayer { get; private set; }
        public PluginManager Plugins { get; private set; }
        public PistonUI UI { get; private set; }

        private bool _init;

        public void Start()
        {
            if (!_init)
                Init();

            Server.StartServer();
        }

        public void Stop()
        {
            Server.StopServer();
        }

        public void Init()
        {
            if (_init)
                return;

            Logger.Write("Initializing Torch");
            _init = true;
            Server = new ServerManager();
            Multiplayer = new MultiplayerManager(this);
            Plugins = new PluginManager();
            UI = new PistonUI();

            Server.SessionLoaded += Plugins.LoadAllPlugins;
            Server.InitSandbox();
            SteamHelper.Init();
            UI.PropGrid.SetObject(MySandboxGame.ConfigDedicated);
        }

        public void Reset()
        {
            Logger.Write("Resetting Torch");
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
