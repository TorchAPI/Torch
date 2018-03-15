using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Navigation;
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
        public TorchUI uiSource;
        private TorchConfig _torchConfig;

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
            _themes["Animated Dark theme"] = new ResourceDictionary() { Source = new Uri(@"/Themes/Dark Theme Animated.xaml", UriKind.Relative) };

            _themes["Light theme"] = new ResourceDictionary() { Source = new Uri(@"/Themes/Light Theme.xaml", UriKind.Relative) };
            _themes["Light theme animated"] = new ResourceDictionary() { Source = new Uri(@"/Themes/Light Theme Animated.xaml", UriKind.Relative) };

            _themes["Torch Theme"] = new ResourceDictionary() { Source = new Uri(@"/Views/Resources.xaml", UriKind.Relative) };

            if (null == System.Windows.Application.Current)
            {
                new System.Windows.Application();
            }
        }

        public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            var boxText = box.SelectedItem.ToString();

            ChangeTheme(_themes[boxText].Source);

            if (_torchConfig != null)
                _torchConfig.LastUsedTheme = boxText;
        }

        public void ChangeTheme(Uri uri)
        {
            uiSource.Resources.MergedDictionaries.Clear();
            uiSource.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public void SetConfig(TorchConfig config)
        {
            _torchConfig = config;

            if (_themes.ContainsKey(config.LastUsedTheme))
                ChangeTheme(_themes[config.LastUsedTheme].Source);
        }
    }
}
