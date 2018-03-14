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

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ThemeControl.xaml
    /// </summary>
    public partial class ThemeControl : UserControl, INotifyPropertyChanged
    {
        public List<string> Themes
        {
            get => _themes.Keys.ToList();
        }

        private Dictionary<string, ResourceDictionary> _themes = new Dictionary<string, ResourceDictionary>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ThemeControl()
        {
            InitializeComponent();
            this.DataContext = this;

            _themes["Dark theme"] = new ResourceDictionary() { Source = new Uri(@"/Themes/Dark Theme.xaml", UriKind.Relative) };
            _themes["Light theme"] = new ResourceDictionary() { Source = new Uri(@"/Themes/Light Theme.xaml", UriKind.Relative) };
        }

        public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            var boxText = box.SelectedItem.ToString();

            ChangeTheme(_themes[boxText].Source);
        }

        public void ChangeTheme(Uri uri)
        {

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
        }
    }
}
