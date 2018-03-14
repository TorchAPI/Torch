using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.ViewModels;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for PluginsControl.xaml
    /// </summary>
    public partial class PluginsControl : UserControl
    {
        private ITorchServer _server;
        private PluginManager _plugins;

        public PluginsControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            _server = server;
            _server.Initialized += Server_Initialized;
        }

        private void Server_Initialized(ITorchServer obj)
        {
            Dispatcher.InvokeAsync(() =>
            {
                _plugins = _server.Managers.GetManager<PluginManager>();
                var pluginManager = new PluginManagerViewModel(_plugins);
                DataContext = pluginManager;
            });

        }

        private void OpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (_plugins?.PluginDir != null)
                Process.Start(_plugins.PluginDir);
        }
    }
}
