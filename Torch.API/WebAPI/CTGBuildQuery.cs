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
        private const string ARTIFACT_PATH = "ctg/build/torch-server.zip";
        private const string API_PATH = "ctg/json/info.json";
        private const string BRANCH_QUERY = "https://torchapi.com/" + API_PATH;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static CTGBuildQuery _instance;
        public static CTGBuildQuery Instance => _instance ?? (_instance = new CTGBuildQuery());
        private HttpClient _client;

        private CTGBuildQuery()
        {
            _client = new HttpClient();
        }

        public async Task<Job> GetLatestVersion(string branch)
        {
            var h = await _client.GetAsync(string.Format(BRANCH_QUERY, branch));
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"'{branch}' Branch query failed with code {h.StatusCode}");
                if(h.StatusCode == HttpStatusCode.NotFound)
                    Log.Error("This likely means you're trying to update a branch that is not public on Jenkins. Sorry :(");
                return null;
            }

            string r = await h.Content.ReadAsStringAsync();

            GameVersionInfo response;
            try
            {
                response = JsonConvert.DeserializeObject<GameVersionInfo>(r);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize branch response!");
                return null;
            }

            h = await _client.GetAsync($"{response.LastStableBuild.URL}{API_PATH}");
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"Job query failed with code {h.StatusCode}");
                return null;
            }

            r = await h.Content.ReadAsStringAsync();

            Job job;
            try
            {
                job = JsonConvert.DeserializeObject<Job>(r);
                job.BranchName = response.Name;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize job response!");
                return null;
            }
            return job;
        }

        public async Task<bool> DownloadRelease(Job job, string path)
        {
            var h = await _client.GetAsync(job.URL + ARTIFACT_PATH);
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
        public Dictionary<int, MajorVersion> MajorGameVersions { get; set; }
    }

    public class MajorVersion
    {
        public Dictionary<int, BuildInfo> Builds { get; set; }
    }

    public class BuildInfo
    {
        public string Description { get; set; }
        public DateTime Date { get; set; }
    }


    public class CTGBranchResponse
    {
        public string Name;
        public string URL;
        public CTGBuild LastBuild;
        public CTGBuild LastStableBuild;
    }

    public class CTGBuild
    {
        public int Number;
        public string URL;
    }

    public class CTGJob
    {
        public string BranchName;
        public int Number;
        public bool Building;
        public string Description;
        public string Result;
        public string URL;
        private InformationalVersion _version;

        public InformationalVersion Version
        {
            get
            {
                if (_version == null)
                    InformationalVersion.TryParse(Description, out _version);

                return _version;
            }
        }
    }
}
