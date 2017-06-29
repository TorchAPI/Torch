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
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for VoxelMapView.xaml
    /// </summary>
    public partial class VoxelMapView : UserControl
    {
        public VoxelMapView()
        {
            InitializeComponent();
            DataContextChanged += VoxelMapView_DataContextChanged;
        }

        private void VoxelMapView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Task.Run(() => ((VoxelMapViewModel)e.NewValue).UpdateAttachedGrids()).Wait();
        }
    }
}
