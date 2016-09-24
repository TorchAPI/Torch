using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform;
using SteamSDK;
using VRage.Game;

namespace Piston
{
    public static class SteamHelper
    {
        private static Thread _callbackThread;

        public static void Init()
        {
            _callbackThread = new Thread(() =>
            {
                while (true)
                {
                    SteamAPI.Instance.RunCallbacks();
                    Thread.Sleep(100);
                }
            }) {Name = "SteamAPICallbacks"};
            _callbackThread.Start();
        }

        public static MySteamWorkshop.SubscribedItem GetItemInfo(ulong itemId)
        {
            MySteamWorkshop.SubscribedItem item = null;

            using (var mre = new ManualResetEvent(false))
            {
                SteamAPI.Instance.RemoteStorage.GetPublishedFileDetails(itemId, 0, (ioFail, result) =>
                {
                    if (!ioFail && result.Result == Result.OK)
                    {
                        item = new MySteamWorkshop.SubscribedItem
                        {
                            Title = result.Title,
                            Description = result.Description,
                            PublishedFileId = result.PublishedFileId,
                            SteamIDOwner = result.SteamIDOwner,
                            Tags = result.Tags.Split(' '),
                            TimeUpdated = result.TimeUpdated,
                            UGCHandle = result.FileHandle
                        };
                    }
                    else
                    {
                        Logger.Write($"Failed to get item info for {itemId}");
                    }

                    mre.Set();
                });

                mre.WaitOne();
                mre.Reset();

                return item;
            }
        }

        public static SteamUGCDetails GetItemDetails(ulong itemId)
        {
            SteamUGCDetails details = default(SteamUGCDetails);
            using (var mre = new ManualResetEvent(false))
            {
                SteamAPI.Instance.UGC.RequestUGCDetails(itemId, 0, (b, result) =>
                {
                    if (!b && result.Details.Result == Result.OK)
                        details = result.Details;
                    else
                        Logger.Write($"Failed to get item details for {itemId}");

                    mre.Set();
                });

                mre.WaitOne();
                mre.Reset();
            }

            return details;
        }

        public static MyObjectBuilder_Checkpoint.ModItem GetModItem(ulong modId)
        {
            var details = GetItemDetails(modId);
            return new MyObjectBuilder_Checkpoint.ModItem(null, modId, details.Title);
        }

        public static MyObjectBuilder_Checkpoint.ModItem GetModItem(SteamUGCDetails details)
        {
            return new MyObjectBuilder_Checkpoint.ModItem(null, details.PublishedFileId, details.Title);
        }
    }
}
