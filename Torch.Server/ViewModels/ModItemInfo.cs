﻿using System;
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
using Torch.Utils.SteamWorkshopTools;

namespace Torch.Server.ViewModels
{
    /// <summary>
    /// Wrapper around VRage.Game.Objectbuilder_Checkpoint.ModItem 
    /// that holds additional meta information
    /// (e.g. workshop description)
    /// </summary>
    public class ModItemInfo : INotifyPropertyChanged
    {
        MyObjectBuilder_Checkpoint.ModItem _modItem;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Human friendly name of the mod
        /// </summary>
        public string FriendlyName
        {
            get { return _modItem.FriendlyName;  }
            set {
                _modItem.FriendlyName = value;
                OnPropertyChanged(nameof(FriendlyName));
            }
        }

        /// <summary>
        /// Workshop ID of the mod
        /// </summary>
        public ulong PublishedFileId
        {
            get { return _modItem.PublishedFileId; }
            set
            {
                _modItem.PublishedFileId = value;
                OnPropertyChanged(nameof(PublishedFileId));
            }
        }

        /// <summary>
        /// Local filename of the mod
        /// </summary>
        public string Name
        {
            get { return _modItem.Name; }
            set
            {
                _modItem.FriendlyName = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Whether or not the mod was added
        /// because another mod depends on it
        /// </summary>
        public bool IsDependency
        {
            get { return _modItem.IsDependency; }
            set
            {
                _modItem.IsDependency = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Constructor, returns a new ModItemInfo instance
        /// </summary>
        /// <param name="mod">The wrapped mod</param>
        public ModItemInfo(MyObjectBuilder_Checkpoint.ModItem mod)
        {
            _modItem = mod;
        }

        private string _description;
        /// <summary>
        /// Workshop description of the mod
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        /// <summary>
        /// Retrieve information about the
        /// wrapped mod from the workhop asynchronously
        /// via the Steam web API.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateModInfoAsync()
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

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Raise PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been changed</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
