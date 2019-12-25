using System.Collections.Generic;
using VRage.Game;

namespace Torch.Server.ViewModels
{
    public class WorldConfigurationViewModel : ViewModel
    {
        private readonly MyObjectBuilder_WorldConfiguration _worldConfiguration;
        private SessionSettingsViewModel _sessionSettings;

        public WorldConfigurationViewModel(MyObjectBuilder_WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
            _sessionSettings = new SessionSettingsViewModel(worldConfiguration.Settings);
        }

        public static implicit operator MyObjectBuilder_WorldConfiguration(WorldConfigurationViewModel model)
        {
            return model._worldConfiguration;
        }
        
        public List<MyObjectBuilder_Checkpoint.ModItem> Mods { get => _worldConfiguration.Mods; set => SetValue(ref _worldConfiguration.Mods, value); }
        
        public SessionSettingsViewModel Settings
        {
            get => _sessionSettings;
            set
            {
                SetValue(ref _sessionSettings, value);
                _worldConfiguration.Settings = _sessionSettings;
            }
        }
    }
}