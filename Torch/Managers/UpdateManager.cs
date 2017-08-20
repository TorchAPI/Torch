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

        private async Task<Tuple<Version, string>> GetLatestRelease(string owner, string name)
        {
            try
            {
                var latest = await _gitClient.Repository.Release.GetLatest(owner, name).ConfigureAwait(false);
                if (latest == null)
                    return new Tuple<Version, string>(new Version(), null);

                var zip = latest.Assets.FirstOrDefault(x => x.Name.Contains(".zip"));
                var versionName = Regex.Match(latest.TagName, "(\\d+\\.)+\\d+").ToString();
                if (string.IsNullOrWhiteSpace(versionName))
                {
                    _log.Warn("Unable to parse tag {0} for {1}/{2}", latest.TagName, owner, name);
                    versionName = "0.0";
                }
                return new Tuple<Version, string>(new Version(versionName), zip?.BrowserDownloadUrl);
            }
            catch (Exception e)
            {
                _log.Error($"An error occurred getting release information for '{owner}/{name}'");
                _log.Error(e);
                return new Tuple<Version, string>(new Version(), null);
            }
        }

        public async Task CheckAndUpdatePlugin(PluginManifest manifest)
        {
            if (!Torch.Config.GetPluginUpdates)
                return;

            try
            {
                var name = manifest.Repository.Split('/');
                if (name.Length != 2)
                {
                    _log.Error($"'{manifest.Repository}' is not a valid GitHub repository.");
                    return;
                }

                var currentVersion = new Version(manifest.Version);
                var releaseInfo = await GetLatestRelease(name[0], name[1]).ConfigureAwait(false);
                if (releaseInfo.Item1 > currentVersion)
                {
                    _log.Warn($"Updating {manifest.Repository} from version {currentVersion} to version {releaseInfo.Item1}");
                    var updateName = Path.Combine(_fsManager.TempDirectory, $"{name[0]}_{name[1]}.zip");
                    var updatePath = Path.Combine(_torchDir, "Plugins");
                    await new WebClient().DownloadFileTaskAsync(new Uri(releaseInfo.Item2), updateName).ConfigureAwait(false);
                    UpdateFromZip(updateName, updatePath);
                    File.Delete(updateName);
                }
                else
                {
                    _log.Info($"{manifest.Repository} is up to date. ({currentVersion})");
                }
            }
            catch (Exception e)
            {
                _log.Error($"An error occured downloading the plugin update for {manifest.Repository}.");
                _log.Error(e);
            }
        }

        private async void CheckAndUpdateTorch()
        {
            if (!Torch.Config.GetTorchUpdates)
                return;

            try
            {
                var releaseInfo = await GetLatestRelease("TorchAPI", "Torch").ConfigureAwait(false);
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
