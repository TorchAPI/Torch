using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Torch.API.WebAPI
{
    public class PluginQuery
    {
        private const string ALL_QUERY = "https://torchapi.com/api/plugins/";
        private const string PLUGIN_QUERY = "https://torchapi.com/api/plugins/?guid={0}";
        private readonly HttpClient _client;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PluginQuery _instance;
        public static PluginQuery Instance => _instance ??= new();

        private PluginQuery()
        {
            _client = new();
        }

        public async Task<PluginsResponse> QueryAll()
        {
            return (PluginsResponse) await _client.GetFromJsonAsync(ALL_QUERY, typeof(PluginsResponse), CancellationToken.None);
        }

        public Task<PluginItem> QueryOne(Guid guid)
        {
            return QueryOne(guid.ToString());
        }

        public async Task<PluginItem> QueryOne(string guid)
        {
            using var res = await _client.GetAsync(string.Format(PLUGIN_QUERY, guid));
            if (!res.IsSuccessStatusCode)
                return null;
            return await res.Content.ReadFromJsonAsync<PluginItem>();
        }

        public Task<bool> DownloadPlugin(Guid guid, string path = null)
        {
            return DownloadPlugin(guid.ToString(), path);
        }

        public async Task<bool> DownloadPlugin(string guid, string path = null)
        {
            var item = await QueryOne(guid);
            if (item is null) return false;
            return await DownloadPlugin(item, path);
        }

        public async Task<bool> DownloadPlugin(PluginItem item, string path = null)
        {
            try
            {
                path ??= Path.Combine(Directory.GetCurrentDirectory(), "Plugins", $"{item.Name}.zip");
                
                if (item.Versions.Length == 0)
                {
                    Log.Error($"Selected plugin {item.Name} does not have any versions to download!");
                    return false;
                }
                var version = item.Versions.FirstOrDefault(v => v.Version == item.LatestVersion);
                if (version is null)
                {
                    Log.Error($"Could not find latest version for selected plugin {item.Name}");
                    return false;
                }
                var s = await _client.GetStreamAsync(version.Url);

                if(File.Exists(path))
                    File.Delete(path);

                await using var f = File.Create(path);
                await s.CopyToAsync(f);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download plugin!");
            }

            return true;
        }
    }

    public record PluginsResponse(PluginItem[] Plugins);

    public record PluginItem(Guid Id, string Name, string Author, string Description, string LatestVersion,
        VersionItem[] Versions)
    {
        [JsonIgnore]
        public bool Installed { get; set; }
    }

    public record VersionItem(string Version, string Note, [property: JsonPropertyName("is_beta")] bool IsBeta,
        string Url);
}
