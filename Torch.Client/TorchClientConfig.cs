using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Client
{
    public class TorchClientConfig : ITorchConfig
    {
        // How do we want to handle client side config?  It's radically different than the server.
        public bool GetPluginUpdates { get; set; } = false;
        public bool GetTorchUpdates { get; set; } = false;
        public string InstanceName { get; set; } = "TorchClient";
        public string InstancePath { get; set; }
        public bool NoUpdate { get; set; } = true;
        public List<string> Plugins { get; set; }
        public bool ShouldUpdatePlugins { get; } = false;
        public bool ShouldUpdateTorch { get; } = false;
        public int TickTimeout { get; set; }
        public bool Autostart { get; set; } = false;
        public bool ForceUpdate { get; set; } = false;
        public bool NoGui { get; set; } = false;
        public bool RestartOnCrash { get; set; } = false;
        public string WaitForPID { get; set; } = null;

        public bool Save(string path = null)
        {
            return true;
        }
    }
}
