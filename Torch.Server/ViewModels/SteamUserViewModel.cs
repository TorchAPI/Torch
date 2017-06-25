using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamSDK;

namespace Torch.Server.ViewModels
{
    public class SteamUserViewModel : ViewModel
    {
        public string Name { get; }
        public ulong SteamId { get; }

        public SteamUserViewModel(ulong id)
        {
            SteamId = id;
            Name = SteamAPI.Instance.Friends.GetPersonaName(id);
        }

        public SteamUserViewModel() : this(0) { }
    }
}
