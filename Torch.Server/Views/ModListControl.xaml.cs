using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NLog;
using Torch.API;
using Torch.Server.Managers;
using Torch.API.Managers;
using Torch.Server.ViewModels;
using Torch.Utils;
using Torch.Views;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ModListControl.xaml
    /// </summary>
    public partial class ModListControl : UserControl
    {
        private static Logger Log = LogManager.GetLogger(nameof(ModListControl));
        private InstanceManager _instanceManager;
        private readonly ITorchConfig _config;
        private ConfigDedicatedViewModel _viewModel;

        //private List<BindingExpression> _bindingExpressions = new List<BindingExpression>();
        /// <summary>
        /// Constructor for ModListControl 
        /// </summary>
        public ModListControl()
        {
            InitializeComponent();
#pragma warning disable CS0618
            _instanceManager = TorchBase.Instance.Managers.GetManager<InstanceManager>();
            _config = TorchBase.Instance.Config;
#pragma warning restore CS0618
            _instanceManager.InstanceLoaded += _instanceManager_InstanceLoaded;
            //var mods = _instanceManager.DedicatedConfig?.Mods;
            //if( mods != null)
            //    DataContext = new ObservableCollection<MyObjectBuilder_Checkpoint.ModItem>();
            DataContext = _instanceManager.DedicatedConfig?.Mods;
            UgcServiceTypeBox.ItemsSource = new[]
            {
                new KeyValuePair<string, string>("Steam", "steam"),
                new KeyValuePair<string, string>("Mod.Io", "mod.io")
            };
            // Gets called once all children are loaded
            //Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyStyles));
        }


        private void _instanceManager_InstanceLoaded(ConfigDedicatedViewModel obj)
        {
            Dispatcher.InvokeAsync(() =>
            {
                _viewModel = obj;
                DataContext = obj;
                UpdateLayout();
                Task.Run(async () =>
                {
                    await obj.UpdateAllModInfosAsync();
                    Log.Info("Instance loaded.");
                });
            });
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _instanceManager.SaveConfig();
        }


        private void AddBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryExtractId(AddModIdTextBox.Text, out ulong id))
            {
                var mod = new ModItemInfo(ModItemUtils.Create(id, UgcServiceTypeBox.SelectedValue?.ToString()));
                
                _instanceManager.DedicatedConfig.Mods.Add(mod);
                Task.Run(mod.UpdateModInfoAsync)
                    .ContinueWith(_ =>
                    {
                        _instanceManager.DedicatedConfig.Save();
                    });
                AddModIdTextBox.Text = "";
            }
            else
            {
                AddModIdTextBox.BorderBrush = Brushes.Red;
                Log.Warn("Invalid mod id!");
                MessageBox.Show("Invalid mod id!");
            }
        }
        private void RemoveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var modList = _viewModel.Mods;
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

        private void ModList_Selected(object sender, SelectedCellsChangedEventArgs e)
        {
            if( e.AddedCells.Count > 0)
                ModList.SelectedItem = e.AddedCells[0].Item;
        }

        private void BulkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new CollectionEditor();

            //let's see just how poorly we can do this
            var modList = _viewModel.Mods.ToList();
            var idList = modList.Select(m => m.ToString()).ToList();
            var tasks = new List<Task>();
            //blocking
            editor.Edit<string>(idList, "Mods");

            modList.Clear();
            modList.AddRange(idList.Select(id =>
            {
                if (!ModItemUtils.TryParse(id, out var item))
                    return null;
                
                var info = new ModItemInfo(item);
                tasks.Add(Task.Run(info.UpdateModInfoAsync));
                return info;
            }).Where(b => b is not null));
            
            _instanceManager.DedicatedConfig.Mods.Clear();
            foreach (var mod in modList)
                _instanceManager.DedicatedConfig.Mods.Add(mod);

            Task.Run(async () =>
            {
                await Task.WhenAll(tasks);
                _instanceManager.DedicatedConfig.Save();
            });
        }

        private void UgcServiceTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string) UgcServiceTypeBox.SelectedValue == UGCServiceType.Steam.ToString() &&
                _config.UgcServiceType == UGCServiceType.EOS)
                MessageBox.Show("Steam workshop is not available with current ugc service!");
        }
    } 
}
