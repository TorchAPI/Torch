using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Torch.API;
using Torch.API.Plugins;
using Torch.Server.Views;

namespace Torch.Server.ViewModels
{
    public class PluginViewModel
    {
        public UserControl Control { get; }
        public string Name { get; }
        public ITorchPlugin Plugin { get; }

        public PluginViewModel(ITorchPlugin plugin)
        {
            Plugin = plugin;

            if (Plugin is IWpfPlugin p)
                Control = p.GetControl();

            Name = $"{plugin.Name} ({plugin.Version})";

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Control.Resources.MergedDictionaries.Clear();
            this.Control.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
