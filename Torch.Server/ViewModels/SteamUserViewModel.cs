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
        private string _name;
        public string Name { get => _name; set { } }

        private ulong _id;

        public ulong SteamId
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
                //TODO: resolve user name
                OnPropertyChanged(nameof(Name));
            }
        }

        public SteamUserViewModel(ulong id)
        {
            SteamId = id;
        }

        public SteamUserViewModel() : this(0) { }
    }
}
