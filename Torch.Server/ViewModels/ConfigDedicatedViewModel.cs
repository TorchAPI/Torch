using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.Server.ViewModels
{
    public class ConfigDedicatedViewModel : ViewModel
    {
        private MyConfigDedicated<MyObjectBuilder_SessionSettings> _config;

        public ConfigDedicatedViewModel()
        {
            _config = new MyConfigDedicated<MyObjectBuilder_SessionSettings>("");
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
            Administrators = new ObservableCollection<string>(_config.Administrators);
            Banned = new ObservableCollection<ulong>(_config.Banned);
            Mods = new ObservableCollection<ulong>(_config.Mods);
        }

        public ConfigDedicatedViewModel(MyConfigDedicated<MyObjectBuilder_SessionSettings> configDedicated)
        {
            _config = configDedicated;
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
            Administrators = new ObservableCollection<string>(_config.Administrators);
            Banned = new ObservableCollection<ulong>(_config.Banned);
            Mods = new ObservableCollection<ulong>(_config.Mods);
        }

        public SessionSettingsViewModel SessionSettings { get; }

        public ObservableCollection<string> WorldPaths { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Administrators { get; }
        public ObservableCollection<ulong> Banned { get; }
        public ObservableCollection<ulong> Mods { get; }

        public int AsteroidAmount
        {
            get { return _config.AsteroidAmount; }
            set { _config.AsteroidAmount = value; OnPropertyChanged(); }
        }

        public ulong GroupId
        {
            get { return _config.GroupID; }
            set { _config.GroupID = value; OnPropertyChanged(); }
        }

        public bool IgnoreLastSession
        {
            get { return _config.IgnoreLastSession; }
            set { _config.IgnoreLastSession = value; OnPropertyChanged(); }
        }

        public string IP
        {
            get { return _config.IP; }
            set { _config.IP = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get { return _config.ServerPort; }
            set { _config.ServerPort = value; OnPropertyChanged(); }
        }

        public string ServerName
        {
            get { return _config.ServerName; }
            set { _config.ServerName = value; OnPropertyChanged(); }
        }

        public bool PauseGameWhenEmpty
        {
            get { return _config.PauseGameWhenEmpty; }
            set { _config.PauseGameWhenEmpty = value; OnPropertyChanged(); }
        }

        public string PremadeCheckpointPath
        {
            get { return _config.PremadeCheckpointPath; }
            set { _config.PremadeCheckpointPath = value; OnPropertyChanged(); }
        }

        public string LoadWorld
        {
            get { return _config.LoadWorld; }
            set { _config.LoadWorld = value; OnPropertyChanged(); }
        }

        public int SteamPort
        {
            get { return _config.SteamPort; }
            set { _config.SteamPort = value; OnPropertyChanged(); }
        }

        public string WorldName
        {
            get { return _config.WorldName; }
            set { _config.WorldName = value; OnPropertyChanged(); }
        }
    }
}
