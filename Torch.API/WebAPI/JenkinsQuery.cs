using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NLog;
using Torch.API.Utils;
using Version = SemanticVersioning.Version;

namespace Torch.API.WebAPI
{
    public class JenkinsQuery
    {
        private const string BRANCH_QUERY = "http://136.243.80.164:2690/job/Torch/job/{0}/" + API_PATH;
        private const string ARTIFACT_PATH = "artifact/bin/torch-server.zip";
        private const string API_PATH = "api/json";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static JenkinsQuery _instance;
        public static JenkinsQuery Instance => _instance ??= new JenkinsQuery();
        private HttpClient _client;

        private JenkinsQuery()
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

            var branchResponse = await h.Content.ReadFromJsonAsync<BranchResponse>();

            if (branchResponse is null)
            {
                Log.Error("Error reading branch response");
                return null;
            }
            
            h = await _client.GetAsync($"{branchResponse.LastStableBuild.Url}{API_PATH}");
            if (h.IsSuccessStatusCode) 
                return await h.Content.ReadFromJsonAsync<Job>();
            
            Log.Error($"Job query failed with code {h.StatusCode}");
            return null;

        }

        public async Task<bool> DownloadRelease(Job job, string path)
        {
            var h = await _client.GetAsync(job.Url + ARTIFACT_PATH);
            if (!h.IsSuccessStatusCode)
            {
                Log.Error($"Job download failed with code {h.StatusCode}");
                return false;
            }
            var s = await h.Content.ReadAsStreamAsync();
#if !NETFRAMEWORK
            await using var fs = new FileStream(path, FileMode.Create);
#else
            using var fs = new FileStream(path, FileMode.Create);
#endif
            await s.CopyToAsync(fs);
            return true;
        }

    }

    public record BranchResponse(string Name, string Url, Build LastBuild, Build LastStableBuild);

    public record Build(int Number, string Url);

    public record Job(int Number, bool Building, string Description, string Result, string Url,
        [property: JsonConverter(typeof(SemanticVersionConverter))] Version Version);
}
