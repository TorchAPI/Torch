using System;

namespace Torch.Utils.SteamWorkshopTools
{
    public class PublishedItemDetails
    {
        public ulong PublishedFileId;
        public uint Views;
        public uint Subscriptions;
        public DateTime TimeUpdated;
        public DateTime TimeCreated;
        public string Description;
        public string Title;
        public string FileUrl;
        public long FileSize;
        public string FileName;
        public ulong ConsumerAppId;
        public ulong CreatorAppId;
        public ulong Creator;
        public string[] Tags;
    }
}
