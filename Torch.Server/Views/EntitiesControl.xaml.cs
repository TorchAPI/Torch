using System;
using System.Collections.Generic;
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
using Torch.Server.ViewModels;
using Torch.Server.ViewModels.Blocks;
using Torch.Server.ViewModels.Entities;
using Torch.Server.Views.Blocks;
using Torch.Server.Views.Entities;
using VRage.Game.ModAPI;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for EntitiesControl.xaml
    /// </summary>
    public partial class EntitiesControl : UserControl
    {
        public EntityTreeViewModel Entities { get; set; }

        public EntitiesControl()
        {
            InitializeComponent();
            Entities = new EntityTreeViewModel(this);
            DataContext = Entities;
            Entities.Init();
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is EntityViewModel vm)
            {
                Entities.CurrentEntity = vm;
                if (e.NewValue is GridViewModel gvm)
                    EditorFrame.Content = new Entities.GridView {DataContext = gvm};
                if (e.NewValue is BlockViewModel bvm)
                    EditorFrame.Content = new BlockView {DataContext = bvm};
                if (e.NewValue is VoxelMapViewModel vvm)
                    EditorFrame.Content = new VoxelMapView {DataContext = vvm};
            }
            else
            {
                Entities.CurrentEntity = null;
                EditorFrame.Content = null;
            }
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            if (Entities.CurrentEntity?.Entity is IMyCharacter)
                return;
            TorchBase.Instance.Invoke(() => Entities.CurrentEntity?.Delete());
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            TorchBase.Instance.Invoke(() => Entities.CurrentEntity?.Entity.Physics?.ClearSpeed());
        }

        private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
        {
            //Exact item that was expanded.
            var item = (TreeViewItem)e.OriginalSource;
            if (item.DataContext is ILazyLoad l)
                l.Load();
        }
    }
}
