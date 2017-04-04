using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Torch.API;
using Torch.API.Plugins;

namespace Torch.Server.ViewModels
{
    public class PluginViewModel
    {
        public UserControl Control
        {
            get
            {
                if (Plugin is IWpfPlugin p)
                    return p.GetControl();

                return null;
            }
        }

        public string Name { get; }
        public ITorchPlugin Plugin { get; }

        public PluginViewModel(ITorchPlugin plugin)
        {
            Plugin = plugin;
            Name = $"{plugin.Name} ({plugin.Version})";
        }
    }
}
