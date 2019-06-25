using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NLog;
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

        private static Logger _log = LogManager.GetCurrentClassLogger();

        public PluginViewModel(ITorchPlugin plugin)
        {
            Plugin = plugin;

            if (Plugin is IWpfPlugin p)
            {
                try
                {
                    Control = p.GetControl();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Exception loading interface for plugin {Plugin.Name}! Plugin interface will not be available!");
                    Control = null;
                }
            }
            
            Name = $"{plugin.Name} ({plugin.Version})";

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            if (this.Control == null)
                return;

            this.Control.Resources.MergedDictionaries.Clear();
            this.Control.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
