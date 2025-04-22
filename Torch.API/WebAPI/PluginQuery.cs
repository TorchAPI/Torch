using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace Torch.API.WebAPI
{
    public class PluginQuery
    {
        public static bool IsApiReachable;
        
#if DEBUG
        private const string ALL_QUERY = "https://torchapi.com/api/plugins?inclcudeArchived=true";
#else
        private const string ALL_QUERY = "https://torchapi.com/api/plugins";
#endif

        private const string PLUGIN_QUERY = "https://torchapi.com/api/plugins/search/{0}";
        private readonly HttpClient _client;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PluginQuery _instance;
        public static PluginQuery Instance => _instance ?? (_instance = new PluginQuery());

        private PluginQuery()
        {
            _client = new HttpClient();
        }

        public async Task<PluginResponse> QueryAll()
        {
            var h = await _client.GetAsync(ALL_QUERY);
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"Plugin query returned response {h.StatusCode}");
                return null;
            }

            var r = await h.Content.ReadAsStringAsync();

            PluginResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<PluginResponse>(r);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize plugin query response!");
                return null;
            }
            return response;
        }

        public static async Task<bool> TestApiConnection()
        {
            Log.Warn("Testing connection to API");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    HttpResponseMessage response = await client.GetAsync(ALL_QUERY);
            
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Warn($"API responded with status: {response.StatusCode}");
                        return false;
                    }

                    IsApiReachable = true;
                    return true;
                }
            }
            catch (HttpRequestException e)
            {
                Log.Error("Error testing API connection.");
                return false;
            }
            catch (TaskCanceledException)
            {
                Log.Error("API request timed out.");
                return false;
            }
            catch (Exception)
            {
                Log.Error("Unexpected error testing API connection.");
                return false;
            }
        }


        public async Task<PluginFullItem> QueryOne(Guid guid)
        {
            return await QueryOne(guid.ToString());
        }

        public async Task<PluginFullItem> QueryOne(string guid)
        {

            var h = await _client.GetAsync(string.Format(PLUGIN_QUERY, guid));
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"Plugin query returned response {h.StatusCode}");
                return null;
            }

            var r = await h.Content.ReadAsStringAsync();

            PluginFullItem response;
            try
            {
                response = JsonConvert.DeserializeObject<PluginFullItem>(r);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize plugin query response!");
                return null;
            }
            return response;
        }

        public async Task<bool> DownloadPlugin(Guid guid, string path = null)
        {
            return await DownloadPlugin(guid.ToString(), path);
        }

        public async Task<bool> DownloadPlugin(string guid, string path = null)
        {
            var item = await QueryOne(guid);
            if (item == null) return false;
            return await DownloadPlugin(item, path);
        }

        public async Task<bool> DownloadPlugin(PluginFullItem item, string path = null)
        {
            try
            {
                path = path ?? $"Plugins\\{item.Name}.zip";
                string relpath = Path.GetDirectoryName(path);

                Directory.CreateDirectory(relpath);

                var h = await _client.GetAsync(string.Format(PLUGIN_QUERY, item.ID));
                string res = await h.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PluginFullItem>(res);
                if (response.Versions.Length == 0)
                {
                    Log.Error($"Selected plugin {item.Name} does not have any versions to download!");
                    return false;
                }
                var version = response.Versions.FirstOrDefault(v => v.Version == response.LatestVersion);
                if (version == null)
                {
                    Log.Error($"Could not find latest version for selected plugin {item.Name}");
                    return false;
                }
                var s = await _client.GetStreamAsync(version.URL);

                if(File.Exists(path))
                    File.Delete(path);

                using (var f = File.Create(path))
                {
                    await s.CopyToAsync(f);
                    await f.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download plugin!");
            }

            return true;
        }
    }

    public class PluginResponse
    {
        public PluginItem[] Plugins;
        public int Count;
    }

    public class PluginItem
    {
        public string ID;
        public string Name { get; set; }
        public string Author;
        public string Description;
        public string LatestVersion;
        public bool Installed { get; set; } = false;

        public override string ToString()
        {
            return Name;
        }
    }

    public class PluginFullItem : PluginItem
    {
        public VersionItem[] Versions;
    }

    public class VersionItem
    {
        public string Version;
        public string Note;
        public bool IsBeta;
        public string URL;
    }
}
