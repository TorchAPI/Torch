using System.Windows;
using System.Windows.Controls;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for EntityControlsView.xaml
    /// </summary>
    public partial class EntityControlsView : ItemsControl
    {
        public EntityControlsView()
        {
            InitializeComponent();

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
