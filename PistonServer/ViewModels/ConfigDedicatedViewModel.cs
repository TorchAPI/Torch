using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Piston.Server.ViewModels
{
    public class ConfigDedicatedViewModel : ViewModel
    {
        public IMyConfigDedicated Config { get; }

        public MTObservableCollection<string> Administrators { get; } = new MTObservableCollection<string>();
        public MTObservableCollection<ulong> BannedPlayers { get; } = new MTObservableCollection<ulong>();

        public int AsteroidAmount
        {
            get { return Config.AsteroidAmount; }
            set { Config.AsteroidAmount = value; OnPropertyChanged(); }
        }

        public ConfigDedicatedViewModel(IMyConfigDedicated config)
        {
            Config = config;
            Config.Administrators.ForEach(x => Administrators.Add(x));
            Config.Banned.ForEach(x => BannedPlayers.Add(x));
        }

        public void FlushConfig()
        {
            Config.Administrators = Administrators.ToList();
        }
    }
}
