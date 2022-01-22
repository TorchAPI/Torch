using System;
using System.Threading.Tasks;
using NLog;
using VRage.Game;
using Torch.Utils;
using VRage.GameServices;

namespace Torch.Server.ViewModels
{
    /// <summary>
    /// Wrapper around VRage.Game.Objectbuilder_Checkpoint.ModItem 
    /// that holds additional meta information
    /// (e.g. workshop description)
    /// </summary>
    public class ModItemInfo : ViewModel
    {
        MyObjectBuilder_Checkpoint.ModItem _modItem;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Human friendly name of the mod
        /// </summary>
        public string FriendlyName
        {
            get => _modItem.FriendlyName;
            set => SetValue(ref _modItem.FriendlyName, value);
        }

        /// <summary>
        /// Workshop ID of the mod
        /// </summary>
        public ulong PublishedFileId
        {
            get => _modItem.PublishedFileId;
            set => SetValue(ref _modItem.PublishedFileId, value);
        }

        /// <summary>
        /// Local filename of the mod
        /// </summary>
        public string Name
        {
            get => _modItem.Name;
            set => SetValue(ref _modItem.FriendlyName, value);
        }

        /// <summary>
        /// Whether or not the mod was added
        /// because another mod depends on it
        /// </summary>
        public bool IsDependency
        {
            get => _modItem.IsDependency;
            set => SetValue(ref _modItem.IsDependency, value);
        }

        private string _description;
        /// <summary>
        /// Workshop description of the mod
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetValue(ref _description, value);
        }

        public string UgcService
        {
            get => _modItem.PublishedServiceName;
            set => SetValue(ref _modItem.PublishedServiceName, value);
        }

        /// <summary>
        /// Constructor, returns a new ModItemInfo instance
        /// </summary>
        /// <param name="mod">The wrapped mod</param>
        public ModItemInfo(MyObjectBuilder_Checkpoint.ModItem mod)
        {
            _modItem = mod;
        }

        /// <summary>
        /// Retrieve information about the
        /// wrapped mod from the workhop asynchronously
        /// via the Steam web API.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateModInfoAsync()
        {
            /*if (UgcService.ToLower() == "mod.io")
                return true;

            MyWorkshopItem modInfo;
            try
            {
                modInfo = await WorkshopQueryUtils.GetModInfo(_modItem);
            }
            catch( Exception e ) 
            {
                Log.Error(e);
                return false;
            }
            
            Log.Info("Mod Info successfully retrieved!");
            FriendlyName = modInfo.Title;
            Description = modInfo.Description;*/
            return true;
        }

        public override string ToString()
        {
            return $"{PublishedFileId}-{UgcService}";
        }
    }
}
