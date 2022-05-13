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
    public class SiteQuery
    {
        private const string SITE_QUERY_URL = "https://torchapi.com/api/";
        private const string SERVER_QUERY_URL = "https://torchapi.com/api/servers/";
        private readonly HttpClient _client;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static SiteQuery _instance;
        public static SiteQuery Instance => _instance ?? (_instance = new SiteQuery());

        private SiteQuery()
        {
            _client = new HttpClient();
        }

        public async Task<PrivateQueryResponse> GetIsHigherAccess(string identifier)
        {
            var httpResponse = new HttpResponseMessage();
            var values = new Dictionary<string, string>
            {
                {"identifier", identifier},
                {"action", "check_access"}
            };
            var content = new FormUrlEncodedContent(values);
            httpResponse = await _client.PostAsync(SERVER_QUERY_URL, content);
            
            PrivateQueryResponse privResponse;
            try
            {
                privResponse = JsonConvert.DeserializeObject<PrivateQueryResponse>(await httpResponse.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize PrivateQueryResponse response!");
                return null;
            }
            return privResponse;
        }

        public async Task<PluginUsageResponse> EnsureUserCanAccessPlugin(string identifier, string plugin)
        {
            var httpResponse = new HttpResponseMessage();
            var values = new Dictionary<string, string>
            {
                {"identifier", identifier},
                {"action", "check_plugin_access"},
                {"guid", plugin}
            };
            var content = new FormUrlEncodedContent(values);
            httpResponse = await _client.PostAsync(SERVER_QUERY_URL, content);

            PluginUsageResponse privResponse;
            try
            {
                privResponse =
                    JsonConvert.DeserializeObject<PluginUsageResponse>(await httpResponse.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize PrivateQueryResponse response!");
                return null;
            }

            return privResponse;
        }
    }

    public class PrivateQueryResponse
    {
        public bool HigherAccess;
    }

    public class PluginUsageResponse
    {
        public bool CanUse;
    }
}