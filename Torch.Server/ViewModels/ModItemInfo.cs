using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using NLog;
using VRage.Game;
using Torch.Server.Annotations;
using SteamWorkshopService;
using SteamWorkshopService.Types;

namespace Torch.Server.ViewModels
{
    public class ModItemInfo : INotifyPropertyChanged
    {
        MyObjectBuilder_Checkpoint.ModItem _modItem;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string FriendlyName
        {
            get { return _modItem.FriendlyName;  }
            set {
                _modItem.FriendlyName = value;
                OnPropertyChanged(nameof(FriendlyName));
            }
        }

        public ulong PublishedFileId
        {
            get { return _modItem.PublishedFileId; }
            set
            {
                _modItem.PublishedFileId = value;
                OnPropertyChanged(nameof(PublishedFileId));
            }
        }

        public string Name
        {
            get { return _modItem.Name; }
            set
            {
                _modItem.FriendlyName = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public ModItemInfo(MyObjectBuilder_Checkpoint.ModItem mod)
        {
            _modItem = mod;
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public async Task<bool> UpdateModInfoAsync(Action<string> messageHandler = null)
        {
            var msg = "";
            var workshopService = WebAPI.Instance;
            PublishedItemDetails modInfo = null;
            try
            {
                modInfo = (await workshopService.GetPublishedFileDetails(new ulong[] { PublishedFileId }))?[PublishedFileId];
            }
            catch( Exception e ) 
            {
                Log.Error(e.Message);
            }
            if (modInfo == null)
            {
                Log.Error($"Failed to retrieve mod with workshop id '{PublishedFileId}'!");
                return false;
            }
            //else if (!modInfo.Tags.Contains(""))
            else
            {
                Log.Info($"Mod Info successfully retrieved!");
                FriendlyName = modInfo.Title;
                Description = modInfo.Description;
                //Name = modInfo.FileName;
                return true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
