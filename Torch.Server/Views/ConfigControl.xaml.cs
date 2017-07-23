using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml.Serialization;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Torch.Server.Managers;
using Torch.Server.ViewModels;
using Torch.Views;
using VRage;
using VRage.Dedicated;
using VRage.Game;
using VRage.ObjectBuilders;
using Path = System.IO.Path;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ConfigControl.xaml
    /// </summary>
    public partial class ConfigControl : UserControl
    {
        private InstanceManager _instanceManager;

        public ConfigControl()
        {
            InitializeComponent();
            _instanceManager = TorchBase.Instance.GetManager<InstanceManager>();
            DataContext = _instanceManager.DedicatedConfig;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.SaveConfig();
        }

        private void RemoveLimit_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (BlockLimitViewModel)((Button)sender).DataContext;
            _instanceManager.DedicatedConfig.SessionSettings.BlockLimits.Remove(vm);
        }

        private void AddLimit_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.DedicatedConfig.SessionSettings.BlockLimits.Add(new BlockLimitViewModel(_instanceManager.DedicatedConfig.SessionSettings, "", 0));
        }

        private void NewWorld_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Feature coming soon :)");
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //The control doesn't update the binding before firing the event.
            if (e.AddedItems.Count > 0)
            {
                _instanceManager.SelectWorld((string)e.AddedItems[0]);
            }
        }
    }
}
