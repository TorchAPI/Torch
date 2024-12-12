using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }

        private void VoxelMapView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Task.Run(() => ((VoxelMapViewModel)e.NewValue).UpdateAttachedGrids()).Wait();
        }
    }
}
