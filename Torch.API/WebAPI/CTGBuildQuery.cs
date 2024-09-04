using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace Torch.API.WebAPI
{
    public class CTGBuildQuery
    {
        private const string WEB_URL = "https://torchapi.com/";
        private const string ARTIFACT_PATH = "ctg/build";
        private const string API_PATH = "ctg/info";
        private const string BRANCH_QUERY = WEB_URL + API_PATH;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static CTGBuildQuery _instance;
        public static CTGBuildQuery Instance => _instance ?? (_instance = new CTGBuildQuery());
        private HttpClient _client;

        private CTGBuildQuery()
        {
            _client = new HttpClient();
        }

        public async Task<CTGBuild> GetLatestVersion(string branch)
        {
            HttpResponseMessage responseMessage = await _client.GetAsync(BRANCH_QUERY);
            if (!responseMessage.IsSuccessStatusCode)
            {
                Log.Error($"'{branch}' Branch query failed with code {responseMessage.StatusCode}");
                if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Error("This likely means you're trying to update a branch that is not available. Sorry :(");
                }
                return null;
            }

            string jsonResponse = await responseMessage.Content.ReadAsStringAsync();
            GameVersionInfo gameVersionInfo;
            try 
            { 
                gameVersionInfo = JsonConvert.DeserializeObject<GameVersionInfo>(jsonResponse); 
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize branch response!");
                return null;
            }

            if (!gameVersionInfo.MajorGameVersions.Any()) 
            { 
                Log.Error("No major game versions found!"); 
                return null; 
            }

            KeyValuePair<string, MajorVersion> latestMajorVersion = gameVersionInfo.MajorGameVersions.Last();
            if (!latestMajorVersion.Value.Builds.Any()) 
            { 
                Log.Error("No builds found in the latest major version!"); 
                return null; 
            }

            KeyValuePair<string, CTGBuild> latestBuildEntry = latestMajorVersion.Value.Builds.Last();
            CTGBuild latestBuild = latestBuildEntry.Value;
            latestBuild.Version = $"{latestMajorVersion.Key}-{latestBuildEntry.Key}";

            return latestBuild;
        }

        public async Task<bool> DownloadRelease(CTGBuild build, string path)
        {
            
            
            var h = await _client.GetAsync(WEB_URL + ARTIFACT_PATH + $"/{build.Version}");
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"Job download failed with code {h.StatusCode}");
                return false;
            }
            var s = await h.Content.ReadAsStreamAsync();
            using (var fs = new FileStream(path, FileMode.Create))
            {
                await s.CopyToAsync(fs);
                await fs.FlushAsync();
            }
            return true;
        }

    }
    

    public class GameVersionInfo
    {
        public Dictionary<string, MajorVersion> MajorGameVersions { get; set; }
    }

    public class MajorVersion
    {
        public Dictionary<string, CTGBuild> Builds { get; set; }
    }

    public class CTGBuild
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
    }
}
