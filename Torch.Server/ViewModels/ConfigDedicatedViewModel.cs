using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Utils;
using Torch.Collections;
using Torch.Server.Managers;
using VRage.Game;
using Torch.Utils.SteamWorkshopTools;

namespace Torch.Server.ViewModels
{
    public class ConfigDedicatedViewModel : ViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MyConfigDedicated<MyObjectBuilder_SessionSettings> _config;
        public MyConfigDedicated<MyObjectBuilder_SessionSettings> Model => _config;

        public ConfigDedicatedViewModel() : this(new MyConfigDedicated<MyObjectBuilder_SessionSettings>(""))
        {

        }

        public ConfigDedicatedViewModel(MyConfigDedicated<MyObjectBuilder_SessionSettings> configDedicated)
        {
            _config = configDedicated;
            //_config.IgnoreLastSession = true;
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
            Task.Run(() => UpdateAllModInfosAsync());
        }

        public void Save(string path = null)
        {
            Validate();

            _config.SessionSettings = _sessionSettings;
            // Never ever
            //_config.IgnoreLastSession = true;
            _config.Save(path);
        }

        public bool Validate()
        {
            if (SelectedWorld == null)
            {
                Log.Warn($"{nameof(SelectedWorld)} == null");
                return false;
            }

            if (LoadWorld == null)
            {
                Log.Warn($"{nameof(LoadWorld)} == null");
                return false;
            }

            return true;
        }

        private SessionSettingsViewModel _sessionSettings;
        public SessionSettingsViewModel SessionSettings { get => _sessionSettings; set { _sessionSettings = value; OnPropertyChanged(); } }

        public MtObservableList<WorldViewModel> Worlds { get; } = new MtObservableList<WorldViewModel>();
        private WorldViewModel _selectedWorld;
        public WorldViewModel SelectedWorld
        {
            get => _selectedWorld;
            set
            {
                SetValue(ref _selectedWorld, value);
                LoadWorld = _selectedWorld?.WorldPath;
            }
        }

        public async Task UpdateAllModInfosAsync(Action<string> messageHandler = null)
        {
            if (Mods.Count() == 0)
                return;

            var ids = Mods.Select(m => m.PublishedFileId);
            var workshopService = WebAPI.Instance;
            Dictionary<ulong, PublishedItemDetails> modInfos = null;

            try
            {
                modInfos = (await workshopService.GetPublishedFileDetails(ids.ToArray()));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return;
            }

            Log.Info($"Mods Info successfully retrieved!");

            foreach (var mod in Mods)
            {
                if (!modInfos.ContainsKey(mod.PublishedFileId) || modInfos[mod.PublishedFileId] == null)
                {
                    Log.Error($"Failed to retrieve info for mod with workshop id '{mod.PublishedFileId}'!");
                }
                //else if (!modInfo.Tags.Contains(""))
                else
                {
                    mod.FriendlyName = modInfos[mod.PublishedFileId].Title;
                    mod.Description = modInfos[mod.PublishedFileId].Description;
                    //mod.Name = modInfos[mod.PublishedFileId].FileName;
                }
            }

        }

        public List<string> Administrators { get => _config.Administrators; set => SetValue(x => _config.Administrators = x, value); }

        public List<ulong> Banned { get => _config.Banned; set => SetValue(x => _config.Banned = x, value); }

        private MtObservableList<ModItemInfo> _mods = new MtObservableList<ModItemInfo>();
        public MtObservableList<ModItemInfo> Mods
        {
            get => _mods;
            set
            {
                SetValue(x => _mods = x, value);
                Task.Run(() => UpdateAllModInfosAsync());
            }
        }

        public List<ulong> Reserved { get => _config.Reserved; set => SetValue(x => _config.Reserved = x, value); }


        public int AsteroidAmount { get => _config.AsteroidAmount; set => SetValue(x => _config.AsteroidAmount = x, value); }

        public ulong GroupId { get => _config.GroupID; set => SetValue(x => _config.GroupID = x, value); }

        public string IP { get => _config.IP; set => SetValue(x => _config.IP = x, value); }

        public int Port { get => _config.ServerPort; set => SetValue(x => _config.ServerPort = x, value); }

        public string ServerName { get => _config.ServerName; set => SetValue(x => _config.ServerName = x, value); }

        public string ServerDescription { get => _config.ServerDescription; set => SetValue(x => _config.ServerDescription = x, value); }

        public bool PauseGameWhenEmpty { get => _config.PauseGameWhenEmpty; set => SetValue(x => _config.PauseGameWhenEmpty = x, value); }

        public bool AutodetectDependencies { get => _config.AutodetectDependencies; set => SetValue(x => _config.AutodetectDependencies = x, value); }

        public string PremadeCheckpointPath { get => _config.PremadeCheckpointPath; set => SetValue(x => _config.PremadeCheckpointPath = x, value); }

        public string LoadWorld { get => _config.LoadWorld; set => SetValue(x => _config.LoadWorld = x, value); }

        public int SteamPort { get => _config.SteamPort; set => SetValue(x => _config.SteamPort = x, value); }

        public string WorldName { get => _config.WorldName; set => SetValue(x => _config.WorldName = x, value); }

        //this is a damn server password. I don't care if this is insecure. Bite me.
        private string _password;
        public string Password
        {
            get
            {
                if (string.IsNullOrEmpty(_password))
                {
                    if (string.IsNullOrEmpty(_config.ServerPasswordHash))
                        return string.Empty;
                    return "**********";
                }
                return _password;
            }
            set
            {
                _password = value;
                if(!string.IsNullOrEmpty(value))
                    _config.SetPassword(value);
                else
                {
                    _config.ServerPasswordHash = null;
                    _config.ServerPasswordSalt = null;
                }
            }
        }
    }
}
