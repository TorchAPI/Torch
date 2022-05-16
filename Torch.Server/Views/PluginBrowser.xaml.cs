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
        private static Logger Log = LogManager.GetCurrentClassLogger();

        public MtObservableList<PluginItem> PluginsSource { get; set; } = new MtObservableList<PluginItem>();
        public MtObservableList<PluginItem> Plugins { get; set; } = new MtObservableList<PluginItem>();
        public PluginItem CurrentItem { get; set; }
        public const string PLUGINS_SEARCH_TEXT = "Plugins search...";
        private string PreviousSearchQuery = "";

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
            BindingOperations.EnableCollectionSynchronization(Plugins,_syncLock);
            Task.Run(async () =>
                     {
                         var res = await PluginQuery.Instance.QueryAll();
                         if (res == null)
                             return;
                         foreach (var item in res.Plugins.OrderBy(i => i.Name)) {
                             lock (_syncLock)
                             {
                                 if (installedPlugins.Keys.Contains(Guid.Parse(item.ID)))
                                     item.Installed = true;
                                 Plugins.Add(item);
                                 PluginsList.Dispatcher.Invoke(() => PluginsList.SelectedIndex = 0);
                                 PluginsSource.Add(item);
                             }
                         }
                         CurrentDescription = "Please select a plugin...";
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
            var SelectedItems = PluginsList.SelectedItems;

            foreach(PluginItem PluginItem in SelectedItems)
                TorchBase.Instance.Config.Plugins.Add(new Guid(PluginItem.ID));
                
            TorchBase.Instance.Config.Save();
            Log.Info($"Started to download {SelectedItems.Count} plugin(s)");

            PluginDownloader DownloadProgress = new PluginDownloader(SelectedItems);
            DownloadProgress.Show();
        }
        
        private void UninstallButton_OnClick(object sender, RoutedEventArgs e) {
            var SelectedItems = PluginsList.SelectedItems;
            if(SelectedItems.Cast<PluginItem>().Any(x => x.Installed == false)) {
                MessageBox.Show($"Error! You have selected at least 1 plugin which isnt currently installed. Please de-select and try again!", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show($"Are you sure you want to attempt uninstall of {SelectedItems.Count} plugin(s)?", "Uninstall Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
                foreach(PluginItem PluginItem in SelectedItems) {
                    if(TorchBase.Instance.Config.Plugins.Contains(Guid.Parse(PluginItem.ID))) {
                        TorchBase.Instance.Config.Plugins.Remove(Guid.Parse(PluginItem.ID));

                        string path = $"Plugins\\{PluginItem.Name}.zip";

                        if (File.Exists(path))
                            File.Delete(path);

                        Log.Info($"Uninstalled {PluginItem.Name}");
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
            if (txtPluginsSearch.Text == PLUGINS_SEARCH_TEXT) {
                txtPluginsSearch.Clear();
                txtPluginsSearch.Foreground = Brushes.Black;
                return;
            }
        }

        private void TxtPluginsSearch_LostFocus(object sender, RoutedEventArgs e) {
            if(txtPluginsSearch.Text == "") {
                txtPluginsSearch.Foreground = Brushes.Gray;
                txtPluginsSearch.Text = PLUGINS_SEARCH_TEXT;
                return;
            }
        }

        private void TxtPluginsSearch_TextChanged(object sender, TextChangedEventArgs e) {
            string SearchQueryString = txtPluginsSearch.Text;

            if(SearchQueryString.Length < PreviousSearchQuery.Length) {
                ResetSearchFilter();
            }

            if (SearchQueryString != PLUGINS_SEARCH_TEXT && SearchQueryString != string.Empty) {
                SearchPlugins(SearchQueryString);
            } else {
                ResetSearchFilter();
            }

            PreviousSearchQuery = SearchQueryString;
        }

        private void SearchPlugins(string SearchQueryString) {
            foreach (var plugin in Plugins.Where(p => !p.Name.Contains(SearchQueryString, StringComparison.OrdinalIgnoreCase) &&
                 !p.Author.Contains(SearchQueryString, StringComparison.OrdinalIgnoreCase))) {
                Plugins.Remove(plugin);
            }

            foreach (var plugin in Plugins.Where(p => p.Name.Contains(SearchQueryString, StringComparison.OrdinalIgnoreCase) ||
             p.Author.Contains(SearchQueryString, StringComparison.OrdinalIgnoreCase))) {
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
