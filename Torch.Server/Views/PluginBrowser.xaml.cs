using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using NLog;
using Torch.API.WebAPI;
using Torch.Collections;
using Torch.Server.Annotations;
using Torch.Managers;
using Torch.API.Managers;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for PluginBrowser.xaml
    /// </summary>
    public partial class PluginBrowser : Window, INotifyPropertyChanged
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        public MtObservableList<PluginItem> PluginsSource { get; set; } = new MtObservableList<PluginItem>();
        public MtObservableList<PluginItem> Plugins { get; set; } = new MtObservableList<PluginItem>();
        public PluginItem CurrentItem { get; set; }
        public const string PluginsSearchText = "Plugins search...";
        private string _previousSearchQuery = "";

        private string _description = "Loading data from server, please wait..";
        private static object _syncLock = new object();
        public string CurrentDescription
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public PluginBrowser(IPluginManager pluginManager)
        {
            InitializeComponent();

            var installedPlugins = pluginManager.Plugins;
            BindingOperations.EnableCollectionSynchronization(Plugins, _syncLock);
            Task.Run(async () =>
            {
                try
                {
                    var res = await PluginQuery.Instance.QueryAll();
                    foreach (var item in res.Plugins.OrderBy(i => i.Name)) {
                        lock (_syncLock)
                        {
                            var pluginItem = item with
                            {
                                Description = item.Description.Replace("&lt;", "<").Replace("&gt;", ">"),
                                Installed = installedPlugins.Keys.Contains(item.Id)
                            };
                            Plugins.Add(pluginItem);
                            PluginsSource.Add(pluginItem);
                        }
                    }

                    Dispatcher.Invoke(() => PluginsList.SelectedIndex = 0);
                    CurrentDescription = "Please select a plugin...";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "An Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    throw;
                }
            });

            MarkdownFlow.CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, (sender, e) => OpenUri((string)e.Parameter)));
        }

        public static bool IsValidUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            Uri tmp;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        public static bool OpenUri(string uri)
        {
            if (!IsValidUri(uri))
                return false;
            System.Diagnostics.Process.Start(uri);
            return true;
        }

        private void PluginsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentItem = (PluginItem)PluginsList.SelectedItem;
            if (CurrentItem != null) {
                CurrentDescription = CurrentItem.Description;
                DownloadButton.IsEnabled = !string.IsNullOrEmpty(CurrentItem.LatestVersion);
                UninstallButton.IsEnabled = !string.IsNullOrEmpty(CurrentItem.LatestVersion);
            }
        }

        private void DownloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = PluginsList.SelectedItems;

            foreach(PluginItem pluginItem in selectedItems)
                TorchBase.Instance.Config.Plugins.Add(pluginItem.Id);
                
            TorchBase.Instance.Config.Save();
            _log.Info($"Started to download {selectedItems.Count} plugin(s)");

            PluginDownloader downloadProgress = new PluginDownloader(selectedItems);
            downloadProgress.Show();
        }
        
        private void UninstallButton_OnClick(object sender, RoutedEventArgs e) {
            var selectedItems = PluginsList.SelectedItems;
            if(selectedItems.Cast<PluginItem>().Any(x => x.Installed == false)) {
                MessageBox.Show($"Error! You have selected at least 1 plugin which isnt currently installed. Please de-select and try again!", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show($"Are you sure you want to attempt uninstall of {selectedItems.Count} plugin(s)?", "Uninstall Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
                foreach(PluginItem pluginItem in selectedItems) {
                    if(TorchBase.Instance.Config.Plugins.Contains(pluginItem.Id)) {
                        TorchBase.Instance.Config.Plugins.Remove(pluginItem.Id);

                        string path = $"Plugins\\{pluginItem.Name}.zip";

                        if (File.Exists(path))
                            File.Delete(path);

                        _log.Info($"Uninstalled {pluginItem.Name}");
                    }
                }
                MessageBox.Show($"Plugins removed... Please restart your server for changes to take effect.", "Uninstall Confirmation", MessageBoxButton.OK);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void TxtPluginsSearch_GotFocus(object sender, RoutedEventArgs e) {
            if (TxtPluginsSearch.Text == PluginsSearchText) {
                TxtPluginsSearch.Clear();
                TxtPluginsSearch.Foreground = Brushes.Black;
                return;
            }
        }

        private void TxtPluginsSearch_LostFocus(object sender, RoutedEventArgs e) {
            if(TxtPluginsSearch.Text == "") {
                TxtPluginsSearch.Foreground = Brushes.Gray;
                TxtPluginsSearch.Text = PluginsSearchText;
                return;
            }
        }

        private void TxtPluginsSearch_TextChanged(object sender, TextChangedEventArgs e) {
            string searchQueryString = TxtPluginsSearch.Text;

            if(searchQueryString.Length < _previousSearchQuery.Length) {
                ResetSearchFilter();
            }

            if (searchQueryString != PluginsSearchText && searchQueryString != string.Empty) {
                SearchPlugins(searchQueryString);
            } else {
                ResetSearchFilter();
            }

            _previousSearchQuery = searchQueryString;
        }

        private void SearchPlugins(string searchQueryString) {
            foreach (var plugin in Plugins.Where(p => !p.Name.Contains(searchQueryString, StringComparison.OrdinalIgnoreCase) &&
                 !p.Author.Contains(searchQueryString, StringComparison.OrdinalIgnoreCase))) {
                Plugins.Remove(plugin);
            }

            foreach (var plugin in Plugins.Where(p => p.Name.Contains(searchQueryString, StringComparison.OrdinalIgnoreCase) ||
             p.Author.Contains(searchQueryString, StringComparison.OrdinalIgnoreCase))) {
                if (!Plugins.Contains(plugin))
                    Plugins.Add(plugin);
            }
        }

        private void ResetSearchFilter() {
            Plugins.Clear();
            foreach (var plugin in PluginsSource) {
                if (!Plugins.Contains(plugin))
                    Plugins.Add(plugin);
            }
        }
    }

}
