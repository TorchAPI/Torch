using Steamworks;

namespace Torch.Server.ViewModels
{
    public class SteamUserViewModel : ViewModel
    {
        public string Name { get; }
        public ulong SteamId { get; }

        public SteamUserViewModel(ulong id)
        {
            SteamId = id;
            Name = SteamFriends.GetFriendPersonaName(new CSteamID(id));
        }

        public SteamUserViewModel() : this(0) { }
    }
}
