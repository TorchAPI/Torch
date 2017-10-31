using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Torch.Server.Managers;
using Torch.API.Managers;
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for EntityControlHost.xaml
    /// </summary>
    public partial class EntityControlHost : UserControl
    {
        public EntityControlHost()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ViewModel vmo)
            {
                vmo.PropertyChanged -= DataContext_OnPropertyChanged;
            }
            if (e.NewValue is ViewModel vmn)
            {
                vmn.PropertyChanged += DataContext_OnPropertyChanged;
            }
            RefreshControl();
        }

        private void DataContext_OnPropertyChanged(object sender, PropertyChangedEventArgs pa)
        {
            if (pa.PropertyName.Equals(EntityControlViewModel.SignalPropertyInvalidateControl))
                RefreshControl();
            else if (pa.PropertyName.Equals(nameof(EntityControlViewModel.Hide)))
                RefreshVisibility();
        }

        private Control _currentControl;

        private void RefreshControl()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.InvokeAsync(RefreshControl);
                return;
            }

            _currentControl = DataContext is EntityControlViewModel ecvm
                ? TorchBase.Instance?.Managers.GetManager<EntityControlManager>()?.CreateControl(ecvm)
                : null;
            Content = _currentControl;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.InvokeAsync(RefreshVisibility);
                return;
            }
            Visibility = (DataContext is EntityControlViewModel ecvm) && !ecvm.Hide && _currentControl != null
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
