using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows;
using Torch.API.WebAPI;
using System.ComponentModel;

namespace Torch.Server.Views
{

    /// <summary>
    /// Interaction logic for PluginDownloadProgressBar.xaml
    /// </summary>
    public partial class PluginDownloader : Window
    {

        private bool downloadNoFailures = true;
        private int successfulDownloads = 0;
        private int failedDownloads = 0;
        private IList PluginsToDownload;

        public PluginDownloader(IList SelectedItems) {
            InitializeComponent();
            PluginsToDownload = SelectedItems;
        }


        private void DownloadProgress_ContentRendered(object sender, EventArgs e) {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += DownloadPlugins;
            worker.ProgressChanged += PluginDownloaded;
            worker.RunWorkerCompleted += DownloadCompleted;

            worker.RunWorkerAsync();
        }

        void DownloadPlugins (object sender, DoWorkEventArgs e) {
            var DownloadProgress = 0;
            var PercentChangeOnDownload = 100 / PluginsToDownload.Count;

            foreach (PluginItem PluginItem in PluginsToDownload) {
                if (!Task.Run(async () => await PluginQuery.Instance.DownloadPlugin(PluginItem.ID)).Result) {
                    failedDownloads++;
                    DownloadProgress += PercentChangeOnDownload;
                    (sender as BackgroundWorker).ReportProgress(DownloadProgress);
                    continue;
                }
                DownloadProgress += PercentChangeOnDownload;
                (sender as BackgroundWorker).ReportProgress(DownloadProgress);
                successfulDownloads++;
            }
            (sender as BackgroundWorker).ReportProgress(100);
        }

        void PluginDownloaded(object sender, ProgressChangedEventArgs e) {
            downloadProgress.Value = e.ProgressPercentage;
        }

        void DownloadCompleted(object sender, RunWorkerCompletedEventArgs e) {
            MessageBox.Show(downloadNoFailures ? $"{successfulDownloads} out of {PluginsToDownload.Count} Plugin(s) downloaded successfully! Please restart the server to load changes."
                                    : $"{failedDownloads} out of {PluginsToDownload.Count} Plugin(s) failed to download! See log for details.",
                                    "Plugin Downloader",
                                    MessageBoxButton.OK, downloadNoFailures ? MessageBoxImage.Information : MessageBoxImage.Warning);
            Close();
        }
    }
}
