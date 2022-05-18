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
using Torch.API;
using Torch.API.WebAPI;

namespace Torch.Managers
{
    /// <summary>
    /// Handles updating of the DS and Torch plugins.
    /// </summary>
    public class UpdateManager : Manager
    {
        private Timer _updatePollTimer;
        private string _torchDir = new FileInfo(typeof(UpdateManager).Assembly.Location).DirectoryName;
        private Logger _log = LogManager.GetCurrentClassLogger();
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
        
        private async void CheckAndUpdateTorch()
        {
            if (Torch.Config.NoUpdate || !Torch.Config.GetTorchUpdates)
                return;

            try
            {
                var job = await JenkinsQuery.Instance.GetLatestVersion(Torch.Config.BranchName.ToString());
                if (job == null)
                {
                    _log.Info("Failed to fetch latest version.");
                    return;
                }
                
                if (job.Version > Torch.TorchVersion || (Torch.TorchVersion.Branch != Torch.Config.BranchName.ToString()))
                {
                    _log.Warn($"Updating Torch from version {Torch.TorchVersion} to version {job.Version}");
                    var updateName = Path.Combine(_fsManager.TempDirectory, "torchupdate.zip");
                    //new WebClient().DownloadFile(new Uri(releaseInfo.Item2), updateName);
                    if (!await JenkinsQuery.Instance.DownloadRelease(job, updateName))
                    {
                        _log.Warn("Failed to download new release!");
                        return;
                    }
                    UpdateFromZip(updateName, _torchDir);
                    File.Delete(updateName);
                    _log.Warn($"Torch version {job.Version} has been installed. Please restart to apply update.");
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
                    if(file.Name == "NLog-user.config" && File.Exists(Path.Combine(extractPath, file.FullName)))
                        continue;

                    var targetFile = Path.Combine(extractPath, file.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                    _fsManager.SoftDelete(extractPath, file.FullName);
                    file.ExtractToFile(targetFile, true);
                }

                //zip.ExtractToDirectory(extractPath); //throws exceptions sometimes?
            }
        }

        /// <inheritdoc />
        public override void Detach()
        {
            _updatePollTimer?.Dispose();
        }
    }
}
