using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Utils;
using Torch.Collections;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Game;
using VRage.GameServices;

namespace Torch.Server.ViewModels
{
    public class ConfigDedicatedViewModel : ViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MyConfigDedicated<MyObjectBuilder_SessionSettings> _config;
        public MyConfigDedicated<MyObjectBuilder_SessionSettings> Model => _config;

        public ConfigDedicatedViewModel(MyConfigDedicated<MyObjectBuilder_SessionSettings> configDedicated)
        {
            _config = configDedicated;
            _config.IgnoreLastSession = true;
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
            Task.Run(() => UpdateAllModInfosAsync());
        }

        public void Save(string path = null)
        {
            Validate();

            _config.SessionSettings = SessionSettings;
            _config.IgnoreLastSession = true;
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

        public SessionSettingsViewModel SessionSettings { get; set; }

        public MtObservableList<WorldViewModel> Worlds { get; } = new MtObservableList<WorldViewModel>();
        private WorldViewModel _selectedWorld;
        public WorldViewModel SelectedWorld
        {
            get => _selectedWorld;
            set
            {
                SetValue(ref _selectedWorld, value);
                SessionSettings = value.WorldConfiguration.Settings;
                LoadWorld = _selectedWorld?.WorldPath;
            }
        }

        public Task UpdateAllModInfosAsync()
        {
            return Task.CompletedTask;
            /*if (!Mods.Any())
                return;
            List<MyWorkshopItem> modInfos;
            try
            {
                modInfos = await WorkshopQueryUtils.GetModsInfo(Mods.Select(b =>
                    new MyObjectBuilder_Checkpoint.ModItem(b.PublishedFileId, b.UgcService, b.IsDependency)));
            }
            catch (Exception e)
            {
                Log.Error(e);
                return;
            }

            Log.Info("Mods Info successfully retrieved!");

            foreach (var modItem in Mods
                         .Select(b => new MyObjectBuilder_Checkpoint.ModItem(b.PublishedFileId, b.UgcService))
                         .Except(modInfos.Select(b => new MyObjectBuilder_Checkpoint.ModItem(b.Id, b.ServiceName))))
            {
                Log.Error($"Unable to retreive info about {modItem.PublishedFileId}:{modItem.PublishedServiceName}");
            }*/
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
