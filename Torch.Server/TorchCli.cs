using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Server
{
    public class TorchCli : CommandLine
    {
        public TorchConfig Config { get; set; }

        [Arg("instancepath", "Server data folder where saves and mods are stored.")]
        public string InstancePath { get => Config.InstancePath; set => Config.InstancePath = value; }

        [Arg("noupdate", "Disable automatically downloading game and plugin updates.")]
        public bool NoUpdate { get => !Config.AutomaticUpdates; set => Config.AutomaticUpdates = !value; }

        [Arg("update", "Manually check for and install updates.")]
        public bool Update { get; set; }

        //TODO: backend code for this
        //[Arg("worldpath", "Path to the game world folder to load.")]
        public string WorldPath { get; set; }

        [Arg("autostart", "Start the server immediately.")]
        public bool Autostart { get; set; }

        [Arg("restartoncrash", "Automatically restart the server if it crashes.")]
        public bool RestartOnCrash { get => Config.RestartOnCrash; set => Config.RestartOnCrash = value; }

        [Arg("nogui", "Do not show the Torch UI.")]
        public bool NoGui { get; set; }

        [Arg("silent", "Do not show the Torch UI or the command line.")]
        public bool Silent { get; set; }

        [Arg("waitforpid", "Makes Torch wait for another process to exit.")]
        public string WaitForPID { get; set; }
    }
}
