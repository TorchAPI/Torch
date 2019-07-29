using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Torch.API.Managers;
using Torch.Server.Annotations;
using Torch.Server.Managers;
using Torch.Server.ViewModels;
using Torch.Views;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ConfigControl.xaml
    /// </summary>
    public partial class ConfigControl : UserControl, INotifyPropertyChanged
    {
        private InstanceManager _instanceManager;

        private bool _configValid;
        public bool ConfigValid { get => _configValid; private set { _configValid = value; OnPropertyChanged(); } }

        private List<BindingExpression> _bindingExpressions = new List<BindingExpression>();

        public ConfigControl()
        {
            InitializeComponent();
            _instanceManager = TorchBase.Instance.Managers.GetManager<InstanceManager>();
            _instanceManager.InstanceLoaded += _instanceManager_InstanceLoaded;
            DataContext = _instanceManager.DedicatedConfig;

            // Gets called once all children are loaded
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyStyles));
        }

        private void CheckValid()
        {
            ConfigValid = !_bindingExpressions.Any(x => x.HasError);
        }

        private void ApplyStyles()
        {
            foreach (var textbox in GetAllChildren<TextBox>(this))
            {
                textbox.Style = (Style)Resources["ValidatedTextBox"];
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                if (binding == null)
                    continue;

                _bindingExpressions.Add(binding);
                textbox.TextChanged += (sender, args) =>
                {
                    binding.UpdateSource();
                    CheckValid();
                };

                textbox.LostKeyboardFocus += (sender, args) =>
                {
                    if (binding.HasError)
                        binding.UpdateTarget();
                    CheckValid();
                };

                CheckValid();
            }
        }

        private IEnumerable<T> GetAllChildren<T>(DependencyObject control) where T : DependencyObject
        {
            var children = LogicalTreeHelper.GetChildren(control).OfType<DependencyObject>();
            foreach (var child in children)
            {
                if (child is T t)
                    yield return t;

                foreach (var grandChild in GetAllChildren<T>(child))
                    yield return grandChild;
            }
        }

        private void _instanceManager_InstanceLoaded(ConfigDedicatedViewModel obj)
        {
            Dispatcher.Invoke(() => DataContext = obj);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.SaveConfig();
        }

        private void ImportConfig_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.ImportSelectedWorldConfig();
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NewWorld_OnClick(object sender, RoutedEventArgs e)
        {
            var c  = new WorldGeneratorDialog(_instanceManager);
            c.Show();
        }

        private void RoleEdit_Onlick(object sender, RoutedEventArgs e)
        {
            //var w = new RoleEditor(_instanceManager.DedicatedConfig.SelectedWorld);
            //w.Show();
            var promotedUsers = _instanceManager?.DedicatedConfig?.SelectedWorld?.Checkpoint?.PromotedUsers;
            if (promotedUsers?.Dictionary != null)
            {
                var d = new RoleEditor();
                d.Edit(_instanceManager.DedicatedConfig.SelectedWorld.Checkpoint.PromotedUsers.Dictionary);
                var removed = promotedUsers.Dictionary.Where(p => p.Value == VRage.Game.ModAPI.MyPromoteLevel.None).Select(p => p.Key).ToArray();
                foreach (var id in removed)
                {
                    promotedUsers.Dictionary.Remove(id);
                }
                _instanceManager.DedicatedConfig.Administrators = promotedUsers.Dictionary
                    .Where(p => p.Value == VRage.Game.ModAPI.MyPromoteLevel.Admin || p.Value == VRage.Game.ModAPI.MyPromoteLevel.Owner)
                    .Select(p => p.Key.ToString())
                    .ToList();
            } 
            else
            {
                MessageBox.Show("World not loaded.");
            }
        }
    }
}
