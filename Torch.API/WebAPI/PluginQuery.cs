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
#if DEBUG
        private const string ALL_QUERY = "https://torchapi.com/api/plugins?includeArchived=true";
#else
        private const string ALL_QUERY = "https://torchapi.com/api/plugins";
#endif

        private const string ALL_PRIVATE_QUERY = "https://torchapi.com/api/plugins/private/";
        private const string PLUGIN_QUERY = "https://torchapi.com/api/plugins/item/{0}";
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

        public async Task<PluginFullItem> QueryOne(string guid, bool priv = false, string username = null, string secret = null)
        {

            var h = new HttpResponseMessage();
            if (priv)
            {
                //private query using PostAsync with username,secret and guid params
                var values = new Dictionary<string, string>
                {
                    {"username", username},
                    {"secret", secret},
                    {"guid", guid}
                };
                var content = new FormUrlEncodedContent(values);
                h = await _client.PostAsync(ALL_PRIVATE_QUERY, content);
            }
            else
            {
                h = await _client.GetAsync(string.Format(PLUGIN_QUERY, guid));
            }
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

        public async Task<bool> DownloadPlugin(string guid)
        {
            var item = await QueryOne(guid);
            if (item == null) return false;
            return await DownloadPlugin(item);
        }

        public async Task<bool> DownloadPrivatePlugin(string guid, string username, string secret)
        {
            var item = await QueryOne(guid, true, username, secret);
            if (item == null) return false;
            return await DownloadPlugin(item);
        }

        public async Task<bool> DownloadPlugin(PluginFullItem item, string path = null)
        {
            try
            {
                path = path ?? $"Plugins\\{item.Name}.zip";
                string relpath = Path.GetDirectoryName(path);

                Directory.CreateDirectory(relpath);

                
                var version = item.Versions.FirstOrDefault(v => v.Version == item.LatestVersion);
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
        public bool IsPrivate;
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
