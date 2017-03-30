using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Library.Utils;

namespace Torch.Server.ViewModels
{
    public class SessionSettingsViewModel : ViewModel
    {
        private MyObjectBuilder_SessionSettings _settings;

        public SessionSettingsViewModel()
        {
            _settings = new MyObjectBuilder_SessionSettings();
        }

        public SessionSettingsViewModel(MyObjectBuilder_SessionSettings settings)
        {
            _settings = settings;
        }

        #region Multipliers
        public float InventorySizeMultiplier
        {
            get { return _settings.InventorySizeMultiplier; }
            set { _settings.InventorySizeMultiplier = value; OnPropertyChanged(); }
        }

        public float RefinerySpeedMultiplier
        {
            get { return _settings.RefinerySpeedMultiplier; }
            set { _settings.RefinerySpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float AssemblerEfficiencyMultiplier
        {
            get { return _settings.AssemblerEfficiencyMultiplier; }
            set { _settings.AssemblerEfficiencyMultiplier = value; OnPropertyChanged(); }
        }

        public float AssemblerSpeedMultiplier
        {
            get { return _settings.AssemblerSpeedMultiplier; }
            set { _settings.AssemblerSpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float GrinderSpeedMultiplier
        {
            get { return _settings.GrinderSpeedMultiplier; }
            set { _settings.GrinderSpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float HackSpeedMultiplier
        {
            get { return _settings.HackSpeedMultiplier; }
            set { _settings.HackSpeedMultiplier = value; OnPropertyChanged(); }
        }
        #endregion

        #region NPCs
        public bool EnableDrones
        {
            get { return _settings.EnableDrones; }
            set { _settings.EnableDrones = value; OnPropertyChanged(); }
        }

        public bool EnableEncounters
        {
            get { return _settings.EnableEncounters; }
            set { _settings.EnableEncounters = value; OnPropertyChanged(); }
        }

        public bool EnableSpiders
        {
            get { return _settings.EnableSpiders; }
            set { _settings.EnableSpiders = value; OnPropertyChanged(); }
        }

        public bool EnableWolves
        {
            get { return _settings.EnableWolfs; }
            set { _settings.EnableWolfs = value; OnPropertyChanged(); }
        }

        public bool EnableCargoShips
        {
            get { return _settings.CargoShipsEnabled; }
            set { _settings.CargoShipsEnabled = value; OnPropertyChanged(); }
        }
        #endregion

        #region Environment
        public bool EnableSunRotation
        {
            get { return _settings.EnableSunRotation; }
            set { _settings.EnableSunRotation = value; OnPropertyChanged(); }
        }

        public bool EnableAirtightness
        {
            get { return _settings.EnableOxygenPressurization; }
            set { _settings.EnableOxygenPressurization = value; OnPropertyChanged(); }
        }

        public bool EnableOxygen
        {
            get { return _settings.EnableOxygen; }
            set { _settings.EnableOxygen = value; OnPropertyChanged(); }
        }

        public bool EnableDestructibleBlocks
        {
            get { return _settings.DestructibleBlocks; }
            set { _settings.DestructibleBlocks = value; OnPropertyChanged(); }
        }

        public bool EnableToolShake
        {
            get { return _settings.EnableToolShake; }
            set { _settings.EnableToolShake = value; OnPropertyChanged(); }
        }

        public bool EnableVoxelDestruction
        {
            get { return _settings.EnableVoxelDestruction; }
            set { _settings.EnableVoxelDestruction = value; OnPropertyChanged(); }
        }

        public List<string> EnvironmentHostilityValues => Enum.GetNames(typeof(MyEnvironmentHostilityEnum)).ToList();

        public string EnvironmentHostility
        {
            get { return _settings.EnvironmentHostility.ToString(); }
            set { Enum.TryParse(value, true, out _settings.EnvironmentHostility); OnPropertyChanged(); }
        }

        public bool EnableFlora
        {
            get { return _settings.EnableFlora; }
            set { _settings.EnableFlora = value; OnPropertyChanged(); }
        }
        #endregion

        public List<string> GameModeValues => Enum.GetNames(typeof(MyGameModeEnum)).ToList();

        public string GameMode
        {
            get { return _settings.GameMode.ToString(); }
            set { Enum.TryParse(value, true, out _settings.GameMode); OnPropertyChanged(); }
        }

        public bool EnableAutoHealing
        {
            get { return _settings.AutoHealing; }
            set { _settings.AutoHealing = value; OnPropertyChanged(); }
        }

        public bool EnableCopyPaste
        {
            get { return _settings.EnableCopyPaste; }
            set { _settings.EnableCopyPaste = value; OnPropertyChanged(); }
        }

        public bool ShowPlayerNamesOnHud
        {
            get { return _settings.ShowPlayerNamesOnHud; }
            set { _settings.ShowPlayerNamesOnHud = value; OnPropertyChanged(); }
        }

        public bool EnableThirdPerson
        {
            get { return _settings.Enable3rdPersonView; }
            set { _settings.Enable3rdPersonView = value; OnPropertyChanged(); }
        }

        public bool EnableSpectator
        {
            get { return _settings.EnableSpectator; }
            set { _settings.EnableSpectator = value; OnPropertyChanged(); }
        }

        public bool SpawnWithTools
        {
            get { return _settings.SpawnWithTools; }
            set { _settings.SpawnWithTools = value; OnPropertyChanged(); }
        }

        public bool EnableConvertToStation
        {
            get { return _settings.EnableConvertToStation; }
            set { _settings.EnableConvertToStation = value; OnPropertyChanged(); }
        }

        public bool EnableJetpack
        {
            get { return _settings.EnableJetpack; }
            set { _settings.EnableJetpack = value; OnPropertyChanged(); }
        }

        public bool EnableRemoteOwnerRemoval
        {
            get { return _settings.EnableRemoteBlockRemoval; }
            set { _settings.EnableRemoteBlockRemoval = value; OnPropertyChanged(); }
        }

        public bool EnableRespawnShips
        {
            get { return _settings.EnableRespawnShips; }
            set { _settings.EnableRespawnShips = value; OnPropertyChanged(); }
        }

        public bool EnableScripterRole
        {
            get { return _settings.EnableScripterRole; }
            set { _settings.EnableScripterRole = value; OnPropertyChanged(); }
        }

        public bool EnableRealisticSound
        {
            get { return _settings.RealisticSound; }
            set { _settings.RealisticSound = value; OnPropertyChanged(); }
        }

        public bool ResetOwnership
        {
            get { return _settings.ResetOwnership; }
            set { _settings.ResetOwnership = value; OnPropertyChanged(); }
        }

        public bool DeleteRespawnShips
        {
            get { return _settings.RespawnShipDelete; }
            set { _settings.RespawnShipDelete = value; OnPropertyChanged(); }
        }

        public bool EnableThrusterDamage
        {
            get { return _settings.ThrusterDamage; }
            set { _settings.ThrusterDamage = value; OnPropertyChanged(); }
        }

        public bool EnableWeapons
        {
            get { return _settings.WeaponsEnabled; }
            set { _settings.WeaponsEnabled = value; OnPropertyChanged(); }
        }

        public bool EnableIngameScripts
        {
            get { return _settings.EnableIngameScripts; }
            set { _settings.EnableIngameScripts = value; OnPropertyChanged(); }
        }

        public uint AutosaveInterval
        {
            get { return _settings.AutoSaveInMinutes; }
            set { _settings.AutoSaveInMinutes = value; OnPropertyChanged(); }
        }

        public int FloraDensity
        {
            get { return _settings.FloraDensity; }
            set { _settings.FloraDensity = value; OnPropertyChanged(); }
        }

        public float FloraDensityMultiplier
        {
            get { return _settings.FloraDensityMultiplier; }
            set { _settings.FloraDensityMultiplier = value; OnPropertyChanged(); }
        }

        public short MaxBackupSaves
        {
            get { return _settings.MaxBackupSaves; }
            set { _settings.MaxBackupSaves = value; OnPropertyChanged(); }
        }

        public int MaxBlocksPerPlayer
        {
            get { return _settings.MaxBlocksPerPlayer; }
            set { _settings.MaxBlocksPerPlayer = value; OnPropertyChanged(); }
        }

        public short MaxFloatingObjects
        {
            get { return _settings.MaxFloatingObjects; }
            set { _settings.MaxFloatingObjects = value; OnPropertyChanged(); }
        }

        public int MaxGridSize
        {
            get { return _settings.MaxGridSize; }
            set { _settings.MaxGridSize = value; OnPropertyChanged(); }
        }

        public short MaxPlayers
        {
            get { return _settings.MaxPlayers; }
            set { _settings.MaxPlayers = value; OnPropertyChanged(); }
        }

        public int PhysicsIterations
        {
            get { return _settings.PhysicsIterations; }
            set { _settings.PhysicsIterations = value; OnPropertyChanged(); }
        }

        public float SpawnTimeMultiplier
        {
            get { return _settings.SpawnShipTimeMultiplier; }
            set { _settings.SpawnShipTimeMultiplier = value; OnPropertyChanged(); }
        }

        public float SunRotationInterval
        {
            get { return _settings.SunRotationIntervalMinutes; }
            set { _settings.SunRotationIntervalMinutes = value; OnPropertyChanged(); }
        }

        public int ViewDistance
        {
            get { return _settings.ViewDistance; }
            set { _settings.ViewDistance = value; OnPropertyChanged(); }
        }

        public int WorldSize
        {
            get { return _settings.ViewDistance; }
            set { _settings.WorldSizeKm = value; OnPropertyChanged(); }
        }
    }
}
