using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Octokit;
using Torch.API;
using Torch.Managers;
using VRage.Compression;

namespace Torch.Updater
{
    public class PluginUpdater
    {
        private readonly PluginManager _pluginManager;
        private static readonly Logger Log = LogManager.GetLogger("PluginUpdater");

        public PluginUpdater(PluginManager pm)
        {
            _pluginManager = pm;
        }

        public async Task CheckAndUpdate(PluginManifest manifest, bool force = false)
        {
            Log.Info($"Checking for update at {manifest.Repository}");
            var split = manifest.Repository.Split('/');

            if (split.Length != 2)
            {
                Log.Warn($"Manifest has an invalid repository name: {manifest.Repository}");
                return;
            }

            var gitClient = new GitHubClient(new ProductHeaderValue("Torch"));
            var releases = await gitClient.Repository.Release.GetAll(split[0], split[1]);

            if (releases.Count == 0)
            {
                Log.Debug("No releases in repo");
                return;
            }

            Version currentVersion;
            Version latestVersion;

            try
            {
                currentVersion = new Version(manifest.Version);
                latestVersion = new Version(releases[0].TagName);
            }
            catch (Exception e)
            {
                Log.Warn("Invalid version number on manifest or GitHub release");
                return;
            }

            if (force || latestVersion > currentVersion)
            {
                var webClient = new WebClient();
                var assets = await gitClient.Repository.Release.GetAllAssets(split[0], split[1], releases[0].Id);
                foreach (var asset in assets)
                {
                    if (asset.Name.EndsWith(".zip"))
                    {
                        Log.Debug(asset.BrowserDownloadUrl);
                        var localPath = Path.Combine(Path.GetTempPath(), asset.Name);
                        await webClient.DownloadFileTaskAsync(new Uri(asset.BrowserDownloadUrl), localPath);
                        UnzipPlugin(localPath);
                        Log.Info($"Downloaded update for {manifest.Repository}");
                        return;
                    }
                }
            }
            else
            {
                Log.Info($"{manifest.Repository} is up to date.");
            }
        }

        public void UnzipPlugin(string zipName)
        {
            if (!File.Exists(zipName))
                return;

            MyZipArchive.ExtractToDirectory(zipName, _pluginManager.PluginDir);
        }
    }
}
