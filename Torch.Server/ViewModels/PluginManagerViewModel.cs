using System.Collections.Generic;
using System.Linq;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Collections;

namespace Torch.Server.ViewModels
{
    public class PluginManagerViewModel : ViewModel
    {
        public MtObservableList<PluginViewModel> Plugins { get; } = new MtObservableList<PluginViewModel>();

        private PluginViewModel _selectedPlugin;
        public PluginViewModel SelectedPlugin
        {
            get => _selectedPlugin;
            set { _selectedPlugin = value; OnPropertyChanged(nameof(SelectedPlugin)); }
        }

        public PluginManagerViewModel() { }

        public PluginManagerViewModel(IPluginManager pluginManager)
        {
            foreach (var plugin in pluginManager.OrderBy(x=>x.Name))
                Plugins.Add(new PluginViewModel(plugin));
            pluginManager.PluginsLoaded += PluginManager_PluginsLoaded;
        }

        private void PluginManager_PluginsLoaded(IReadOnlyCollection<ITorchPlugin> obj)
        {
            Plugins.Clear();
            foreach (var plugin in obj)
            {
                Plugins.Add(new PluginViewModel(plugin));
            }
        }
    }
}
