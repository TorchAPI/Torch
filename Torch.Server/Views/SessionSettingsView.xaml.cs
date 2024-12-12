using System.Windows;
using System.Windows.Controls;
using Torch.Server.ViewModels;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for SessionSettingsView.xaml
    /// </summary>
    public partial class SessionSettingsView : UserControl
    {
        public SessionSettingsView()
        {
            InitializeComponent();
        }

        private void RemoveLimit_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (BlockLimitViewModel)((Button)sender).DataContext;
            //_instanceManager.DedicatedConfig.SessionSettings.BlockLimits.Remove(vm);
        }

        private void AddLimit_OnClick(object sender, RoutedEventArgs e)
        {
            //_instanceManager.DedicatedConfig.SessionSettings.BlockLimits.Add(new BlockLimitViewModel(_instanceManager.DedicatedConfig.SessionSettings, "", 0));
        }
    }
}
