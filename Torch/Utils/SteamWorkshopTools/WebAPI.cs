using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using NLog;
using SteamKit2;
using System.Net.Http;

namespace Torch.Utils.SteamWorkshopTools
{
    public class WebAPI
    {
        private static Logger Log = LogManager.GetLogger("SteamWorkshopService");
        public const uint AppID = 244850U;
        public string Username { get; private set; }
        private string password;
        public bool IsReady { get; private set; }
        public bool IsRunning { get; private set; }
        private TaskCompletionSource<bool> logonTaskCompletionSource;

        private SteamClient steamClient;
        private CallbackManager cbManager;
        private SteamUser steamUser;

        private static WebAPI _instance;
        public static WebAPI Instance
        {
            get
            {
                return _instance ?? (_instance = new WebAPI());
            }
        }

        private WebAPI()
        {
            steamClient = new SteamClient();
            cbManager = new CallbackManager(steamClient);

            IsRunning = true;
        }

        public async Task<bool> Logon(string user = "anonymous", string pw = "")
        {
            if (string.IsNullOrEmpty(user))
                throw new ArgumentNullException("User can't be null!");
            if (!user.Equals("anonymous") && !pw.Equals(""))
                throw new ArgumentNullException("Password can't be null if user is not anonymous!");

            Username = user;
            password = pw;

            logonTaskCompletionSource = new TaskCompletionSource<bool>();

            steamUser = steamClient.GetHandler<SteamUser>();
            cbManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            cbManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            cbManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            cbManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            Log.Info("Connecting to Steam...");

            steamClient.Connect();

            await logonTaskCompletionSource.Task;
            return logonTaskCompletionSource.Task.Result;
        }

        public void CancelLogon()
        {
            logonTaskCompletionSource?.SetCanceled();
        }

        public async Task<Dictionary<ulong, PublishedItemDetails>> GetPublishedFileDetails(IEnumerable<ulong> workshopIds)
        {
            //if (!IsReady)
            //    throw new Exception("SteamWorkshopService not initialized!");

            using (dynamic remoteStorage = SteamKit2.WebAPI.GetInterface("ISteamRemoteStorage"))
            {
                KeyValue allFilesDetails = null ;
                remoteStorage.Timeout = TimeSpan.FromSeconds(30);
                allFilesDetails = await Task.Run(delegate {
                    try
                    {
                        return remoteStorage.GetPublishedFileDetails1(
                            itemcount: workshopIds.Count(),
                            publishedfileids: workshopIds,
                            method: HttpMethod.Post);
                        // var ifaceArgs = new Dictionary<string, string>();
                        // ifaceArgs["itemcount"] = workshopIds.Count().ToString();
                        // no idea if that formatting is correct - in fact I get a 404 response
                        // ifaceArgs["publishedfileids"] = string.Join(",", workshopIds);
                        // return remoteStorage.Call(HttpMethod.Post, "GetPublishedFileDetails", args: ifaceArgs);
                    }
                    catch (HttpRequestException e)
                    {
                        Log.Error($"Fetching File Details failed: {e.Message}");
                        return null;
                    }
                });
                if (allFilesDetails == null)
                    return null;
                //fileDetails = remoteStorage.Call(HttpMethod.Post, "GetPublishedFileDetails", 1, new Dictionary<string, string>() { { "itemcount", workshopIds.Count().ToString() }, { "publishedfileids", workshopIds.ToString() } });
                var detailsList = allFilesDetails?.Children.Find((KeyValue kv) => kv.Name == "publishedfiledetails")?.Children;
                var resultCount = allFilesDetails?.GetValueOrDefault<int>("resultcount");
                if( detailsList == null || resultCount == null)
                {
                    Log.Error("Received invalid data: ");
#if DEBUG
                    if(allFilesDetails != null)
                        PrintKeyValue(allFilesDetails);
                    return null;
#endif
                }
                if ( detailsList.Count != workshopIds.Count() || resultCount != workshopIds.Count())
                {
                    Log.Error($"Received unexpected number of fileDetails. Expected: {workshopIds.Count()}, Received: {resultCount}");
                    return null;
                }

                var result = new Dictionary<ulong, PublishedItemDetails>();
                for( int i = 0; i < resultCount; i++ )
                {
                    var fileDetails = detailsList[i];

                    var tagContainer = fileDetails.Children.Find(item => item.Name == "tags");
                    List<string> tags = new List<string>();
                    if (tagContainer != null)
                        foreach (var tagKv in tagContainer.Children)
                        {
                            var tag = tagKv.Children.Find(item => item.Name == "tag")?.Value;
                            if( tag != null)
                                tags.Add(tag);
                        }

                    var publishedFileId = fileDetails.GetValueOrDefault<ulong>("publishedfileid");
                    result[publishedFileId] = new PublishedItemDetails()
                    {
                        PublishedFileId = publishedFileId,
                        Views           = fileDetails.GetValueOrDefault<uint>("views"),
                        Subscriptions   = fileDetails.GetValueOrDefault<uint>("subscriptions"),
                        TimeUpdated     = DateTimeOffset.FromUnixTimeSeconds(fileDetails.GetValueOrDefault<long>("time_updated")).DateTime,
                        TimeCreated     = DateTimeOffset.FromUnixTimeSeconds(fileDetails.GetValueOrDefault<long>("time_created")).DateTime,
                        Description     = fileDetails.GetValueOrDefault<string>("description"),
                        Title           = fileDetails.GetValueOrDefault<string>("title"),
                        FileUrl         = fileDetails.GetValueOrDefault<string>("file_url"),
                        FileSize        = fileDetails.GetValueOrDefault<long>("file_size"),
                        FileName        = fileDetails.GetValueOrDefault<string>("filename"),
                        ConsumerAppId   = fileDetails.GetValueOrDefault<ulong>("consumer_app_id"),
                        CreatorAppId    = fileDetails.GetValueOrDefault<ulong>("creator_app_id"),
                        Creator         = fileDetails.GetValueOrDefault<ulong>("creator"),
                        Tags            = tags.ToArray()
                    };
                }
                return result;
            }
        }

        [Obsolete("Space Engineers has transitioned to Steam's UGC api, therefore this method might not always work!")]
        public async Task DownloadPublishedFile(PublishedItemDetails fileDetails, string dir, string name = null)
        {
            var fullPath = Path.Combine(dir, name);
            if (name == null)
                name = fileDetails.FileName;
            var expectedSize = (fileDetails.FileSize == 0) ? -1 : fileDetails.FileSize;

            using (var client = new WebClient())
            {
                try
                {
                    var downloadTask = client.DownloadFileTaskAsync(fileDetails.FileUrl, Path.Combine(dir, name));
                    DateTime start = DateTime.Now;
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(1000);
                        if (downloadTask.IsCompleted)
                            break;
                    }
                    if ( !downloadTask.IsCompleted )
                    {
                        client.CancelAsync();
                        throw new Exception("Timeout while attempting to downloading published workshop item!");
                    }
                    //var text = await client.DownloadStringTaskAsync(url);
                    //File.WriteAllText(fullPath, text);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to download workshop item! /n" +
                        $"{e.Message} - url: {fileDetails.FileUrl}, path: {Path.Combine(dir, name)}");
                    throw e;
                }
            }

        }

        class Printable
        {
            public KeyValue Data;
            public int Offset;

            public void Print()
            {
                Log.Info($"{new string(' ', Offset)}{Data.Name}: {Data.Value}");
            }
        }

        private static void PrintKeyValue(KeyValue data)
        {

            var dataSet = new Stack<Printable>();
            dataSet.Push(new Printable()
            {
                Data = data,
                Offset = 0
            });
            while (dataSet.Count != 0)
            {
                var printable = dataSet.Pop();
                foreach (var child in printable.Data.Children)
                    dataSet.Push(new Printable()
                    {
                        Data = child,
                        Offset = printable.Offset + 2
                    });
                printable.Print();
            }
        }


#region CALLBACKS
        private void OnConnected( SteamClient.ConnectedCallback callback)
        {
            Log.Info("Connected to Steam! Logging in '{0}'...", Username);
            if( Username == "anonymous" )
                steamUser.LogOnAnonymous();
            else
                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = Username,
                    Password = password
                });
        }

        private void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            Log.Info("Disconnected from Steam");
            IsReady = false;
            IsRunning = false;
        }

        private void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            if( callback.Result != EResult.OK )
            {
                string msg;
                if( callback.Result == EResult.AccountLogonDenied )
                {
                    msg = "Unable to logon to Steam: This account is Steamguard protected.";
                    Log.Warn(msg);
                    logonTaskCompletionSource.SetException(new Exception(msg));
                    IsRunning = false;
                    return;
                }

                msg = $"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}";
                Log.Warn(msg);
                logonTaskCompletionSource.SetException(new Exception(msg));
                IsRunning = false;
                return;
            }
            IsReady = true;
            Log.Info("Successfully logged on!");
            logonTaskCompletionSource.SetResult(true);
        }

        private void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            IsReady = false;
            Log.Info($"Logged off of Steam: {callback.Result}");
        }
#endregion

    }
}
