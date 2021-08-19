using System;
using System.Collections;
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
using Sandbox;
using Torch.API;
using Torch.API.Managers;
using Torch.API.WebAPI;
using Torch.Managers.ChatManager;

namespace Torch.Managers
{
    /// <summary>
    /// Handles updating of the DS and Torch plugins.
    /// </summary>
    public class UpdateManager : Manager
    {
        public bool IsGoingToUpdate { get; private set; }
        
        private Timer _updatePollTimer;
        private string _torchDir = new FileInfo(typeof(UpdateManager).Assembly.Location).DirectoryName;
        private Logger _log = LogManager.GetCurrentClassLogger();
        [Dependency]
        private FilesystemManager _fsManager;
        private ChatManagerServer _chatManager;
        
        public UpdateManager(ITorchBase torchInstance) : base(torchInstance)
        {
            _updatePollTimer = new Timer(TimerElapsed);
        }

        /// <inheritdoc />
        public override void Attach()
        {
            //CheckAndUpdateTorch();
            Torch.GameStateChanged += (_, state) =>
            {
                if (state != TorchGameState.Loaded || IsGoingToUpdate ||
                    !MySandboxGame.ConfigDedicated.AutoUpdateEnabled || Torch.Config.NoUpdate || !Torch.Config.GetTorchUpdates) return;

                _chatManager = Torch.CurrentSession.Managers.GetManager<ChatManagerServer>();
                var delay = TimeSpan.FromMinutes(MySandboxGame.ConfigDedicated.AutoUpdateCheckIntervalInMin);
                _updatePollTimer.Change(delay, delay);
            };
        }

        private void TimerElapsed(object state)
        {
            CheckAndUpdateTorch();
            if (!IsGoingToUpdate) return;
            _updatePollTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Task.Run(async () =>
            {
                _log.Info("Starting countdown for restart");
                foreach (var delay in RestartCountdown())
                {
                    await Task.Delay(delay);
                }

                await Torch.InvokeAsync(() => Torch.Restart());
            });
        }

        private IEnumerable<TimeSpan> RestartCountdown()
        {
            var delay = TimeSpan.FromMinutes(MySandboxGame.ConfigDedicated.AutoUpdateRestartDelayInMin);

            void ReportDelay()
            {
                _chatManager.SendMessageAsSelf(delay.TotalMinutes > 0
                    ? $"Restarting for update in {delay.TotalMinutes:N0} min"
                    : $"Restarting for update in {delay.TotalSeconds:N0} sec");
            }
            
            if (delay.TotalMinutes > 10)
            {
                for (; delay.TotalMinutes > 10; delay -= TimeSpan.FromMinutes(5))
                {
                    ReportDelay();
                    yield return TimeSpan.FromMinutes(5);
                }
            }

            if (delay.TotalMinutes > 1)
            {
                for (; delay.TotalMinutes > 2; delay -= TimeSpan.FromMinutes(1))
                {
                    ReportDelay();
                    yield return TimeSpan.FromMinutes(1);
                }
            }
            
            yield return delay - TimeSpan.FromSeconds(10);
            delay = TimeSpan.FromSeconds(10);

            for (; delay > TimeSpan.Zero; delay -= TimeSpan.FromSeconds(1))
            {
                ReportDelay();
                yield return TimeSpan.FromSeconds(1);
            }
            
            _chatManager.SendMessageAsSelf("Restarting for update.");
        }
        
        private async void CheckAndUpdateTorch()
        {
            if (Torch.Config.NoUpdate || !Torch.Config.GetTorchUpdates)
                return;

            try
            {
                var job = await JenkinsQuery.Instance.GetLatestVersion(Torch.TorchVersion.Branch);
                if (job == null)
                {
                    _log.Info("Failed to fetch latest version.");
                    return;
                }
                
                if (job.Version > Torch.TorchVersion)
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
                    _log.Warn($"Torch version {job.Version} has been installed, please restart Torch to finish the process.");
                    IsGoingToUpdate = true;
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

                    _log.Debug($"Unzipping {file.FullName}");
                    var targetFile = Path.Combine(extractPath, file.FullName);
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
