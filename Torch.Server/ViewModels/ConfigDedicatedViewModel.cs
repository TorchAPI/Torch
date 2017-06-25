using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.Server.ViewModels
{
    public class ConfigDedicatedViewModel : ViewModel
    {
        private static readonly Logger Log = LogManager.GetLogger("Config");
        private MyConfigDedicated<MyObjectBuilder_SessionSettings> _config;

        public ConfigDedicatedViewModel() : this(new MyConfigDedicated<MyObjectBuilder_SessionSettings>(""))
        {

        }

        public ConfigDedicatedViewModel(MyConfigDedicated<MyObjectBuilder_SessionSettings> configDedicated)
        {
            _config = configDedicated;
            SessionSettings = new SessionSettingsViewModel(_config.SessionSettings);
            Administrators = string.Join(Environment.NewLine, _config.Administrators);
            Banned = string.Join(Environment.NewLine, _config.Banned);
            Mods = string.Join(Environment.NewLine, _config.Mods);
        }

        public void Save(string path = null)
        {
            var newline = new [] {Environment.NewLine};

            _config.Administrators.Clear();
            foreach (var admin in Administrators.Split(newline, StringSplitOptions.RemoveEmptyEntries))
                _config.Administrators.Add(admin);

            _config.Banned.Clear();
            foreach (var banned in Banned.Split(newline, StringSplitOptions.RemoveEmptyEntries))
                _config.Banned.Add(ulong.Parse(banned));

            _config.Mods.Clear();
            foreach (var mod in Mods.Split(newline, StringSplitOptions.RemoveEmptyEntries))
            {
                if (ulong.TryParse(mod, out ulong modId))
                    _config.Mods.Add(modId);
                else
                    Log.Warn($"'{mod}' is not a valid mod ID.");
            }

            _config.Save(path);
        }

        public SessionSettingsViewModel SessionSettings { get; }

        public ObservableCollection<string> WorldPaths { get; } = new ObservableCollection<string>();
        public string Administrators { get; set; }
        public string Banned { get; set; }
        public string Mods { get; set; }

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
