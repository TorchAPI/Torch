using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
