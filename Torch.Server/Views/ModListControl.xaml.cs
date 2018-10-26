using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using VRage.Game;
using NLog;
using Torch.Server.Managers;
using Torch.API.Managers;
using Torch.Server.ViewModels;
using Torch.Server.Annotations;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ModListControl.xaml
    /// </summary>
    public partial class ModListControl : UserControl, INotifyPropertyChanged
    {
        private static Logger Log = LogManager.GetLogger("ModListControl");
        private InstanceManager _instanceManager;
        ModItemInfo DraggedMod;
        bool HasOrderChanged = false;
        bool IsSortedByLoadOrder = true;

        //private List<BindingExpression> _bindingExpressions = new List<BindingExpression>();
        public ModListControl()
        {
            InitializeComponent();
            _instanceManager = TorchBase.Instance.Managers.GetManager<InstanceManager>();
            _instanceManager.InstanceLoaded += _instanceManager_InstanceLoaded;
            //var mods = _instanceManager.DedicatedConfig?.Mods;
            //if( mods != null)
            //    DataContext = new ObservableCollection<MyObjectBuilder_Checkpoint.ModItem>();
            DataContext = _instanceManager.DedicatedConfig?.Mods ?? new ObservableCollection<ModItemInfo>();

            // Gets called once all children are loaded
            //Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyStyles));
        }

        private void ResetSorting()
        {
            CollectionViewSource.GetDefaultView(ModList.ItemsSource).SortDescriptions.Clear();
        }


        private void _instanceManager_InstanceLoaded(ConfigDedicatedViewModel obj)
        {
            Log.Info("Instance loaded.");
            //Dispatcher.Invoke(() => DataContext = new ObservableCollection<MyObjectBuilder_Checkpoint.ModItem>(obj.Mods));
            Dispatcher.Invoke(() => DataContext = obj?.Mods ?? new ObservableCollection<ModItemInfo>());
            Dispatcher.Invoke(UpdateLayout);
            Dispatcher.Invoke(() =>
            {
                ((ObservableCollection<ModItemInfo>)DataContext).CollectionChanged += OnModlistUpdate;
            });

        }

        private void OnModlistUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            ModList.Items.Refresh();
            //if (e.Action == NotifyCollectionChangedAction.Remove)
            //    _instanceManager.SaveConfig();
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.SaveConfig();
        }


        private void AddBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryExtractId(AddModIDTextBox.Text, out ulong id))
            {
                var mod = new ModItemInfo(new MyObjectBuilder_Checkpoint.ModItem());
                mod.PublishedFileId = id;
                _instanceManager.DedicatedConfig.Mods.Add(mod);
                Task.Run(mod.UpdateModInfoAsync)
                    .ContinueWith((t) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _instanceManager.DedicatedConfig.Save();
                        });
                    });
                AddModIDTextBox.Text = "";
            }
            else
            {
                AddModIDTextBox.BorderBrush = Brushes.Red;
                Log.Warn("Invalid mod id!");
                MessageBox.Show("Invalid mod id!");
            }
        }
        private void RemoveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var modList = ((ObservableCollection<ModItemInfo>)DataContext);
            if (ModList.SelectedItem is ModItemInfo mod && modList.Contains(mod))
                modList.Remove(mod);
        }

        private bool TryExtractId(string input, out ulong result)
        {
            var match = Regex.Match(input, @"(?<=id=)\d+").Value;

            bool success;
            if (string.IsNullOrEmpty(match))
                success = ulong.TryParse(input, out result);
            else
                success = ulong.TryParse(match, out result);

            return success;
        }

        private void ModList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            Log.Info($"Sorting by '{e.Column.Header}'");
            if (e.Column == ModList.Columns[0])
            {
                var dataView = CollectionViewSource.GetDefaultView(ModList.ItemsSource);
                dataView.SortDescriptions.Clear();
                dataView.Refresh();
                IsSortedByLoadOrder = true;
            }
            else
                IsSortedByLoadOrder = false;
        }

        private void ModList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.Warn("Left button down!");
            //return;

            DraggedMod = (ModItemInfo) TryFindRowAtPoint((UIElement) sender, e.GetPosition(ModList))?.DataContext;

            //DraggedMod = (ModItemInfo) ModList.SelectedItem;
        }

        private static DataGridRow TryFindRowAtPoint(UIElement reference, Point point)
        {
            var element = reference.InputHitTest(point) as DependencyObject;
            if (element == null)
                return null;
            if (element is DataGridRow row)
                return row;
            else
                return TryFindParent<DataGridRow>(element);
        }

        private static T TryFindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent;
            if (child == null)
                return null;
            if (child is ContentElement contentElement)
            {
                parent = ContentOperations.GetParent(contentElement);
                if (parent == null && child is FrameworkContentElement fce)
                    parent = fce.Parent;
            }
            else
            {
                parent = VisualTreeHelper.GetParent(child);
            }

            if (parent is T result)
                return result;
            else
                return TryFindParent<T>(parent);
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (DraggedMod == null)
                return;

            if (!IsSortedByLoadOrder)
            {
                var msg = "Drag and drop is only available when sorted by load order!";
                Log.Warn(msg);
                MessageBox.Show(msg);
                return;
            }

            var targetMod = (ModItemInfo)TryFindRowAtPoint((UIElement)sender, e.GetPosition(ModList))?.DataContext;
            if( targetMod != null && !ReferenceEquals(DraggedMod, targetMod))
            {
                HasOrderChanged = true;
                var modList = (ObservableCollection<ModItemInfo>)DataContext;
                modList.Move(modList.IndexOf(DraggedMod), modList.IndexOf(targetMod));
                ModList.Items.Refresh();
                ModList.SelectedItem = DraggedMod;
            }
        }

        private void ModList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (DraggedMod != null && HasOrderChanged)
                //Log.Info("Dragging over, saving...");
                //_instanceManager.SaveConfig();
            DraggedMod = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ModList_Selected(object sender, SelectedCellsChangedEventArgs e)
        {
            if (DraggedMod != null)
                ModList.SelectedItem = DraggedMod;
            else if( e.AddedCells.Count > 0)
                ModList.SelectedItem = e.AddedCells[0].Item;
        }
    } 
}
