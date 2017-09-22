using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Octokit;
using SteamSDK;
using Torch.API;

namespace Torch.Managers
{
    /// <summary>
    /// Handles updating of the DS and Torch plugins.
    /// </summary>
    public class UpdateManager : Manager
    {
        private Timer _updatePollTimer;
        private GitHubClient _gitClient = new GitHubClient(new ProductHeaderValue("Torch"));
        private string _torchDir = new FileInfo(typeof(UpdateManager).Assembly.Location).DirectoryName;
        private Logger _log = LogManager.GetLogger(nameof(UpdateManager));
        [Dependency]
        private FilesystemManager _fsManager;

        public UpdateManager(ITorchBase torchInstance) : base(torchInstance)
        {
            //_updatePollTimer = new Timer(TimerElapsed, this, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        /// <inheritdoc />
        public override void Attach()
        {
            CheckAndUpdateTorch();
        }

        private void TimerElapsed(object state)
        {
            CheckAndUpdateTorch();
        }

        private async Task<Tuple<Version, string>> TryGetLatestArchiveUrl(string owner, string name)
        {
            try
            {
                var latest = await _gitClient.Repository.Release.GetLatest(owner, name).ConfigureAwait(false);
                if (latest == null)
                    return new Tuple<Version, string>(new Version(), null);

                var zip = latest.Assets.FirstOrDefault(x => x.Name.Contains(".zip"));
                if (zip == null)
                    _log.Error($"Latest release of {owner}/{name} does not contain a zip archive.");
                if (!latest.TagName.TryExtractVersion(out Version version))
                    _log.Error($"Unable to parse version tag for {owner}/{name}");
                return new Tuple<Version, string>(version, zip?.BrowserDownloadUrl);
            }
            catch (Exception e)
            {
                _log.Error($"An error occurred getting release information for '{owner}/{name}'");
                _log.Error(e);
                return default(Tuple<Version, string>);
            }
        }

        private async void CheckAndUpdateTorch()
        {
            // Doesn't work properly or reliably, TODO update when Jenkins is fully configured
            return;

            if (!Torch.Config.GetTorchUpdates)
                return;

            try
            {
                var releaseInfo = await TryGetLatestArchiveUrl("TorchAPI", "Torch").ConfigureAwait(false);
                if (releaseInfo.Item1 > Torch.TorchVersion)
                {
                    _log.Warn($"Updating Torch from version {Torch.TorchVersion} to version {releaseInfo.Item1}");
                    var updateName = Path.Combine(_fsManager.TempDirectory, "torchupdate.zip");
                    new WebClient().DownloadFile(new Uri(releaseInfo.Item2), updateName);
                    UpdateFromZip(updateName, _torchDir);
                    File.Delete(updateName);
                    _log.Warn($"Torch version {releaseInfo.Item1} has been installed, please restart Torch to finish the process.");
                }
                else
                {
                    _log.Info("Torch is up to date.");
                }
            }
            catch (Exception e)
            {
                _log.Error("An error occured downloading the Torch update.");
                _log.Error(e);
            }
        }

        private void UpdateFromZip(string zipFile, string extractPath)
        {
            using (var zip = ZipFile.OpenRead(zipFile))
            {
                foreach (var file in zip.Entries)
                {
                    _log.Debug($"Unzipping {file.FullName}");
                    var targetFile = Path.Combine(extractPath, file.FullName);
                    _fsManager.SoftDelete(targetFile);
                }

                zip.ExtractToDirectory(extractPath);
            }
        }

        /// <inheritdoc />
        public override void Detach()
        {
            _updatePollTimer?.Dispose();
        }
    }
}
