using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;

namespace Torch.Server.ViewModels
{
    public class PluginManagerViewModel : ViewModel
    {
        public MTObservableCollection<PluginViewModel> Plugins { get; } = new MTObservableCollection<PluginViewModel>();

        private PluginViewModel _selectedPlugin;
        public PluginViewModel SelectedPlugin
        {
            get => _selectedPlugin;
            set { _selectedPlugin = value; OnPropertyChanged(); }
        }

        public PluginManagerViewModel() { }

        public PluginManagerViewModel(IPluginManager pluginManager)
        {
            pluginManager.PluginsLoaded += PluginManager_PluginsLoaded;
        }

        private void PluginManager_PluginsLoaded(List<ITorchPlugin> obj)
        {
            Plugins.Clear();
            foreach (var plugin in obj)
            {
                Plugins.Add(new PluginViewModel(plugin));
            }
        }
    }
}
