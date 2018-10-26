using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public ModListControl()
        {
            InitializeComponent();
            _instanceManager = TorchBase.Instance.Managers.GetManager<InstanceManager>();
            _instanceManager.InstanceLoaded += _instanceManager_InstanceLoaded;
            //var mods = _instanceManager.DedicatedConfig?.Mods;
            //if( mods != null)
            //    DataContext = new ObservableCollection<MyObjectBuilder_Checkpoint.ModItem>();
            DataContext = _instanceManager.DedicatedConfig;

            // Gets called once all children are loaded
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyStyles));
        }


        private void _instanceManager_InstanceLoaded(ConfigDedicatedViewModel obj)
        {
            Log.Info("Instance loaded.");
            //Dispatcher.Invoke(() => DataContext = new ObservableCollection<MyObjectBuilder_Checkpoint.ModItem>(obj.Mods));
            Dispatcher.Invoke(() => DataContext = obj);
            Dispatcher.Invoke(UpdateLayout);
        }

        private void ApplyStyles()
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    } 
}
