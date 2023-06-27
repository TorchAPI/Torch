using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;
using Sandbox.Game;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Managers.ChatManager;

namespace Torch.Server.Managers
{
    public class GameUpdateManager : Manager
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly HttpClient _httpClient;
        private const string UpdateCheckUrl = "https://mirror.keenswh.com/news/SpaceEngineersChangelog.xml";
        
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public GameUpdateManager(ITorchBase torchInstance) : base(torchInstance)
        {
            _httpClient = new HttpClient();
        }

        public override void Attach()
        {
            _log.Debug("Starting game update manager");
            base.Attach();
            
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CheckForUpdates();

                    await Task.Delay(TimeSpan.FromSeconds(120), _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }

        public override void Detach()
        {
            _cancellationTokenSource?.Cancel();
            base.Detach();
        }

        private async Task CheckForUpdates()
        {
            _log.Debug("Checking for updates...");

            if (!ShouldCheckForUpdates())
            {
                _log.Debug("Update check skipped");
                return;
            }

            var latestVersion = await GetLatestVersionAsync();

            if (latestVersion == null)
            {
                _log.Debug("Could not fetch the latest game version");
                return;
            }

            var currentVersion = MyPerGameSettings.BasicGameInfo.GameVersion.Value;
            
            if (IsNewerVersion(latestVersion, currentVersion.ToString()))
            {
                await HandleNewVersion(latestVersion);
            }
        }

        private bool ShouldCheckForUpdates()
        {
            return TorchBase.Instance.GameState == TorchGameState.Loaded && TorchBase.Instance.Config.RestartOnGameUpdate;
        }

        private async Task<string> GetLatestVersionAsync()
        {
            var response = await _httpClient.GetStringAsync(UpdateCheckUrl);
            var xml = XDocument.Parse(response);

            var latestEntry = xml.Root.Element("Entry");
            return latestEntry?.Attribute("version")?.Value;
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            var latestVersionParts = latestVersion.Split('.');
            var currentVersionParts = currentVersion.Split('.');

            for (int i = 0; i < Math.Min(latestVersionParts.Length, currentVersionParts.Length); i++)
            {
                if (int.Parse(latestVersionParts[i]) > int.Parse(currentVersionParts[i]))
                {
                    return true;
                }
            }

            return latestVersionParts.Length > currentVersionParts.Length;
        }

        private async Task HandleNewVersion(string latestVersion)
        {
            _log.Debug($"Game update detected!");

            try
            {
                var chatManager = Torch.CurrentSession?.Managers.GetManager<ChatManagerServer>();
                if (chatManager != null)
                {
                    var message = $"A new version of Space Engineers is available! The server will restart in {TorchBase.Instance.Config.GameUpdateRestartDelayMins} minutes to update to version {latestVersion}.";
                    _log.Debug(message);
                    chatManager.SendMessageAsOther("Server", message);
                }

                await Task.Delay(TimeSpan.FromMinutes(TorchBase.Instance.Config.GameUpdateRestartDelayMins), _cancellationTokenSource.Token);

                // Restart the server
                Torch.Restart();
            }
            catch (TaskCanceledException)
            {
                // Task was cancelled, do nothing
            }
            catch
            {
                // ignored
            }
        }
    }
}
