using VRage.Game;

namespace Torch
{
    public class ModViewModel
    {
        public MyObjectBuilder_Checkpoint.ModItem ModItem { get; }
        public string Name => ModItem.Name;
        public string FriendlyName => ModItem.FriendlyName;
        public ulong PublishedFileId => ModItem.PublishedFileId;
        public string Description { get; }

        public ModViewModel(MyObjectBuilder_Checkpoint.ModItem item, string description = "")
        {
            ModItem = item;
            Description = description;
        }

        public static implicit operator MyObjectBuilder_Checkpoint.ModItem(ModViewModel item)
        {
            return item.ModItem;
        }
    }
}
