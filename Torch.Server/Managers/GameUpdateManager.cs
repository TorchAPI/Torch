using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;
using NLog.Fluent;
using Sandbox.Game;
using Torch.API;
using Torch.API.Managers;
using Torch.API.ModAPI;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Server;

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

        /// <summary>
        ///    Starts the game update manager.
        /// </summary>
        public override void Attach()
        {
            _log.Info("Starting game update manager");
            base.Attach();
            int updateIntervalSeconds = TorchBase.Instance.Config.GameUpdateRestartDelayMins * 60;
            
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CheckForUpdates();

                    await Task.Delay(TimeSpan.FromSeconds(updateIntervalSeconds), _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Detach()
        {
            _cancellationTokenSource?.Cancel();
            base.Detach();
        }

        private async Task CheckForUpdates()
        {
            if(TorchBase.Instance.GameState != TorchGameState.Loaded || !TorchBase.Instance.Config.RestartOnGameUpdate)
                return;
            
            var response = await _httpClient.GetStringAsync(UpdateCheckUrl);
            var xml = XDocument.Parse(response);

            var latestEntry = xml.Root.Element("Entry");
            var latestVersion = latestEntry?.Attribute("version")?.Value;
            
            var currentVersion = MyPerGameSettings.BasicGameInfo.GameVersion.Value;
            
            if (latestVersion != null && int.Parse(latestVersion) > currentVersion)
            {
                _log.Warn($"Game update detectected!");
                try
                {
                    Torch.CurrentSession?.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther("Server",
                        $"A new version of Space Engineers is available! The server will restart in {TorchBase.Instance.Config.GameUpdateRestartDelayMins} minutes to update to version {latestVersion}.");

                    // Wait 2 minutes or until the task is cancelled
                    await Task.Delay(TimeSpan.FromMinutes(2), _cancellationTokenSource.Token);

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
}