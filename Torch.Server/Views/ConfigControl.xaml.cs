using System.Windows;
using System.Windows.Controls;
using Torch.API.Managers;
using Torch.Server.Managers;
using Torch.Server.ViewModels;

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
            _instanceManager = TorchBase.Instance.Managers.GetManager<InstanceManager>();
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
