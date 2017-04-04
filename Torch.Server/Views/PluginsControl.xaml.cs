using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Torch.Server.ViewModels;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for PluginsControl.xaml
    /// </summary>
    public partial class PluginsControl : UserControl
    {
        public PluginsControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            var pluginManager = new PluginManagerViewModel(server.Plugins);
            DataContext = pluginManager;
        }
    }
}
