using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace Torch.Updater
{
    public class PluginUpdater
    {
        public async Task CheckForUpdate(PluginManifest manifest)
        {
            var split = manifest.Repository.Split('/');

            if (split.Length != 2)
                return;

            var client = new GitHubClient(new ProductHeaderValue("Torch"));
            var releases = await client.Repository.Release.GetAll(split[0], split[1]);
            var currentVersion = new Version(manifest.Version);
            var latestVersion = new Version(releases[0].TagName);

            if (latestVersion > currentVersion)
            {
                //update
            }
        }
    }
}
