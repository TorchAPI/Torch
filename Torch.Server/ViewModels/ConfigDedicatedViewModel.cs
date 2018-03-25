using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Utils;
using Torch.Collections;
using Torch.Server.Managers;
using VRage.Game;
using VRage.Game.ModAPI;

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
            _config.IgnoreLastSession = true;
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
        }

        public void Save(string path = null)
        {
            Validate();

            _config.SessionSettings = _sessionSettings;
            // Never ever
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

        public List<string> Administrators { get => _config.Administrators; set => SetValue(x => _config.Administrators = x, value); }

        public List<ulong> Banned { get => _config.Banned; set => SetValue(x => _config.Banned = x, value); }

        public List<ulong> Mods { get => _config.Mods; set => SetValue(x => _config.Mods = x, value); }

        public int AsteroidAmount { get => _config.AsteroidAmount; set => SetValue(x => _config.AsteroidAmount = x, value); }

        public ulong GroupId { get => _config.GroupID; set => SetValue(x => _config.GroupID = x, value); }

        public string IP { get => _config.IP; set => SetValue(x => _config.IP = x, value); }

        public int Port { get => _config.ServerPort; set => SetValue(x => _config.ServerPort = x, value); }

        public string ServerName { get => _config.ServerName; set => SetValue(x => _config.ServerName = x, value); }

        public bool PauseGameWhenEmpty { get => _config.PauseGameWhenEmpty; set => SetValue(x => _config.PauseGameWhenEmpty = x, value); }

        public string PremadeCheckpointPath { get => _config.PremadeCheckpointPath; set => SetValue(x => _config.PremadeCheckpointPath = x, value); }

        public string LoadWorld { get => _config.LoadWorld; set => SetValue(x => _config.LoadWorld = x, value); }

        public int SteamPort { get => _config.SteamPort; set => SetValue(x => _config.SteamPort = x, value); }

        public string WorldName { get => _config.WorldName; set => SetValue(x => _config.WorldName = x, value); }
    }
}