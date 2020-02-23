using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        private const string ALL_QUERY = "https://torchapi.net/api/plugins";
        private const string PLUGIN_QUERY = "https://torchapi.net/api/plugins/{0}";
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

        public async Task<bool> DownloadPlugin(Guid guid, string path)
        {
            return await DownloadPlugin(guid.ToString(), path);
        }

        public async Task<bool> DownloadPlugin(string guid, string path)
        {
            var item = await QueryOne(guid);
            return await DownloadPlugin(item, path);
        }

        public async Task<bool> DownloadPlugin(PluginFullItem item, string path)
        {
            try
            {
                if(string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path cannot be null or empty!", nameof(path));
                
                var dir = Directory.CreateDirectory(path);

                foreach(var file in dir.EnumerateFiles())
                    file.Delete();

                foreach(var d in dir.EnumerateDirectories())
                    d.Delete(true);

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

                using (var gz = new ZipArchive(s, ZipArchiveMode.Read))
                {
                    gz.ExtractToDirectory(path);
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
        public string Name;
        public string Author;
        public string Description;
        public string LatestVersion;

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
