using System.Windows;
using System.Windows.Controls;
using NLog;

namespace Torch.Server.Views.Entities
{
    public partial class FloatingObjectsView : UserControl
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public FloatingObjectsView()
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