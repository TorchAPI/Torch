using System;
using System.Collections.Generic;
using System.Drawing;
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
                catch(InvalidOperationException ex)
                {
                    //ignore as its likely a hot reload, we can figure out a better solution in the future.
                    Control = null;
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

        public Brush Color
        {
            get {
                switch (Plugin.State)
                {
                    case PluginState.NotInitialized:
                    case PluginState.MissingDependency:
                    case PluginState.DisabledError:
                        return Brushes.Red;
                    case PluginState.UpdateRequired:
                        return Brushes.DodgerBlue;
                    case PluginState.UninstallRequested:
                        return Brushes.Gold;
                    case PluginState.NotInstalled:
                    case PluginState.DisabledUser:
                        return Brushes.Gray;
                    case PluginState.Enabled:
                        return Brushes.Transparent;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public string ToolTip
        {
            get { switch (Plugin.State)
                {
                    case PluginState.NotInitialized:
                        return "Error during load.";
                    case PluginState.DisabledError:
                        return "Disabled due to error on load.";
                    case PluginState.DisabledUser:
                        return "Disabled.";
                    case PluginState.UpdateRequired:
                        return "Update required.";
                    case PluginState.UninstallRequested:
                        return "Marked for uninstall.";
                    case PluginState.NotInstalled:
                        return "Not installed. Click 'Enable'";
                    case PluginState.Enabled:
                        return string.Empty;
                    case PluginState.MissingDependency:
                        return "Dependency missing. Check the log.";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
