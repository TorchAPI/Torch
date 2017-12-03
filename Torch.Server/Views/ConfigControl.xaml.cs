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
            _instanceManager.InstanceLoaded += _instanceManager_InstanceLoaded;
            DataContext = _instanceManager.DedicatedConfig;
        }

        private void _instanceManager_InstanceLoaded(ConfigDedicatedViewModel obj)
        {
            Dispatcher.Invoke(() => DataContext = obj);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.SaveConfig();
        }

        private void NewWorld_OnClick(object sender, RoutedEventArgs e)
        {
            new WorldGeneratorDialog(_instanceManager).ShowDialog();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //The control doesn't update the binding before firing the event.
            if (e.AddedItems.Count > 0)
            {
                var result = MessageBoxResult.Yes; //MessageBox.Show("Do you want to import the session settings from the selected world?", "Import Config", MessageBoxButton.YesNo);
                var world = (WorldViewModel)e.AddedItems[0];
                _instanceManager.SelectWorld(world.WorldPath, result != MessageBoxResult.Yes);
            }
        }
    }
}
