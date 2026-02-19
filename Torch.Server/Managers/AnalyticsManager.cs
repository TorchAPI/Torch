using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Newtonsoft.Json;

namespace Torch.Server.Managers
{
    /// <summary>
    /// Sends anonymous, GDPR-compliant usage telemetry to torchapi.com every 5 minutes.
    /// Only active when <see cref="TorchConfig.EnableAnalytics"/> is true (opt-in, default false).
    ///
    /// Data sent: server name (on registration only), player count, uptime seconds,
    /// sim speed, Torch version, SE version, active plugin GUIDs.  No PII is collected.
    /// Plugin GUIDs are public identifiers — they are the same GUIDs listed on the Torch
    /// plugin marketplace and do not identify individual users or servers.
    ///
    /// A random UUID token is auto-generated on first registration and stored in Torch.cfg.
    /// To erase all stored data, send DELETE https://torchapi.com/analytics/server/{token}.
    /// </summary>
    public class AnalyticsManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private CancellationTokenSource _cts;

        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(30);
        private const int MaxConsecutiveFailures = 5;
        
        private int _consecutiveFailures;
        private DateTime? _cooldownUntil;
        
        private const string RegisterUrl = "https://torchapi.com/analytics/register";
        private const string ReportUrl   = "https://torchapi.com/analytics/report";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public AnalyticsManager(ITorchBase torchInstance) : base(torchInstance) { }

        public override void Attach()
        {
            base.Attach();

            if (!((TorchConfig)Torch.Config).EnableAnalytics)
            {
                _log.Debug("Analytics disabled — skipping (set EnableAnalytics=true in Torch.cfg to opt in).");
                return;
            }

            _log.Info("=======================================================");
            _log.Info("  TORCH ANALYTICS — ENABLED");
            _log.Info("  Torch is reporting anonymous usage data to torchapi.com");
            _log.Info("  every 5 minutes. Data sent: player count, uptime,");
            _log.Info("  sim speed, Torch version, SE version,");
            _log.Info("  active plugin GUIDs (public identifiers, no PII).");
            _log.Info("  No player names, Steam IDs, IPs, or any PII collected.");
            _log.Info("  Privacy policy : https://torchapi.com/privacy");
            _log.Info("  To opt out     : set EnableAnalytics=false in Torch.cfg");
            _log.Info("=======================================================");

            _cts = new CancellationTokenSource();
            Task.Run(() => RunLoop(_cts.Token), _cts.Token);
        }

        public override void Detach()
        {
            _cts?.Cancel();
            base.Detach();
        }

        private async Task RunLoop(CancellationToken ct)
        {
            try
            {
                // Jitter: spread registrations/reports when many servers restart together
                // after an update. Random delay 0–60 s so servers don't all hit the
                // endpoint at the same millisecond.
                var jitter = TimeSpan.FromSeconds(new Random().Next(0, 60));
                _log.Debug($"Analytics starting in {jitter.TotalSeconds:F0}s (startup jitter).");
                await Task.Delay(jitter, ct);

                // Self-register on first run (token is stored in Torch.cfg)
                if (string.IsNullOrEmpty(((TorchConfig)Torch.Config).AnalyticsToken))
                    await Register(ct);

                while (!ct.IsCancellationRequested)
                {
                    await Report(ct);

                    try
                    {
                        await Task.Delay(Interval, ct);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _log.Warn($"Analytics loop error (non-fatal): {ex.Message}");
            }
        }

        private async Task Register(CancellationToken ct)
        {
            try
            {
                _log.Info("Registering analytics token with torchapi.com…");

                var payload = new { serverName = Torch.Config.InstanceName };
                var json    = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await _http.PostAsync(RegisterUrl, content, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.Warn($"Analytics registration failed: HTTP {(int)resp.StatusCode}");
                    return;
                }

                var body = await resp.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

                if (data != null && data.TryGetValue("token", out var token))
                {
                    ((TorchConfig)Torch.Config).AnalyticsToken = token;
                    // Auto-persisted to Torch.cfg via INotifyPropertyChanged
                    _log.Info($"Analytics token registered: {token}");
                }
                else
                {
                    _log.Warn("Analytics registration response missing token field.");
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Analytics registration error (non-fatal): {ex.Message}");
            }
        }

        private async Task Report(CancellationToken ct)
        {
            try
            {
                // Check if we're in cooldown period
                if (_cooldownUntil.HasValue)
                {
                    if (DateTime.UtcNow < _cooldownUntil.Value)
                    {
                        var remaining = _cooldownUntil.Value - DateTime.UtcNow;
                        _log.Debug($"Analytics report skipped: in cooldown for {remaining.TotalMinutes:F1} more minutes after {MaxConsecutiveFailures} consecutive failures.");
                        return;
                    }
                    
                    // Cooldown expired, reset and try again
                    _log.Info("Analytics cooldown expired, resuming reports.");
                    _cooldownUntil = null;
                    _consecutiveFailures = 0;
                }

                var token = ((TorchConfig)Torch.Config).AnalyticsToken;
                if (string.IsNullOrEmpty(token))
                {
                    _log.Warn("Analytics report skipped: no token. Registration may have failed.");
                    return;
                }

                int   playerCount = 0;
                float simSpeed    = 0f;
                long  uptimeSecs  = (long)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds;

                var session = Torch.CurrentSession;
                if (session?.State == TorchSessionState.Loaded)
                {
                    var mp = session.Managers.GetManager<MultiplayerManagerDedicated>();
                    playerCount = mp?.Players.Count ?? 0;

                    // Read the live sim ratio from TorchServer
                    if (TorchBase.Instance is TorchServer ts)
                        simSpeed = ts.SimulationRatio;
                }

                // Collect active plugin GUIDs — public identifiers only, no PII
                var pluginGuids = new List<string>();
                var pluginManager = Torch.Managers.GetManager<IPluginManager>();
                if (pluginManager != null)
                {
                    foreach (var plugin in pluginManager.Plugins)
                        pluginGuids.Add(plugin.Key.ToString());
                }

                var payload = new
                {
                    token,
                    playerCount,
                    uptimeSeconds = uptimeSecs,
                    simSpeed,
                    torchVersion = Torch.TorchVersion.ToString(),
                    seVersion    = MyPerGameSettings.BasicGameInfo.GameVersion.Value.ToString(),
                    pluginGuids
                };

                var json    = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await _http.PostAsync(ReportUrl, content, ct);
                
                if (resp.IsSuccessStatusCode)
                {
                    // Reset failure counter on success
                    _consecutiveFailures = 0;
                    _log.Debug($"Analytics report sent — players: {playerCount}, plugins: {pluginGuids.Count}, uptime: {uptimeSecs}s, sim: {simSpeed:F2} → HTTP {(int)resp.StatusCode}");
                }
                else
                {
                    _consecutiveFailures++;
                    _log.Warn($"Analytics report failed: HTTP {(int)resp.StatusCode} (failure {_consecutiveFailures}/{MaxConsecutiveFailures})");
                    
                    if (_consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        _cooldownUntil = DateTime.UtcNow.Add(CooldownPeriod);
                        _log.Warn($"Analytics reports disabled for {CooldownPeriod.TotalMinutes} minutes after {MaxConsecutiveFailures} consecutive failures.");
                    }
                }
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _log.Warn($"Analytics report error (non-fatal): {ex.Message} (failure {_consecutiveFailures}/{MaxConsecutiveFailures})");
                
                if (_consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _cooldownUntil = DateTime.UtcNow.Add(CooldownPeriod);
                    _log.Warn($"Analytics reports disabled for {CooldownPeriod.TotalMinutes} minutes after {MaxConsecutiveFailures} consecutive failures.");
                }
            }
        }
    }
}
