using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Toolkit.Collections;
using VRage.Game;
using VRage.Library.Utils;

namespace Torch.Server.ViewModels
{
    public class SessionSettingsViewModel : ViewModel
    {
        private MyObjectBuilder_SessionSettings _settings;

        public SessionSettingsViewModel() : this(new MyObjectBuilder_SessionSettings())
        {
            
        }

        public SessionSettingsViewModel(MyObjectBuilder_SessionSettings settings)
        {
            _settings = settings;
            foreach (var limit in settings.BlockTypeLimits.Dictionary)
                BlockLimits.Add(new BlockLimitViewModel(this, limit.Key, limit.Value));
        }

        public MTObservableCollection<BlockLimitViewModel> BlockLimits { get; } = new MTObservableCollection<BlockLimitViewModel>();

        #region Multipliers
        public float InventorySizeMultiplier
        {
            get => _settings.InventorySizeMultiplier; set { _settings.InventorySizeMultiplier = value; OnPropertyChanged(); }
        }

        public float RefinerySpeedMultiplier
        {
            get => _settings.RefinerySpeedMultiplier; set { _settings.RefinerySpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float AssemblerEfficiencyMultiplier
        {
            get => _settings.AssemblerEfficiencyMultiplier; set { _settings.AssemblerEfficiencyMultiplier = value; OnPropertyChanged(); }
        }

        public float AssemblerSpeedMultiplier
        {
            get => _settings.AssemblerSpeedMultiplier; set { _settings.AssemblerSpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float GrinderSpeedMultiplier
        {
            get => _settings.GrinderSpeedMultiplier; set { _settings.GrinderSpeedMultiplier = value; OnPropertyChanged(); }
        }

        public float HackSpeedMultiplier
        {
            get => _settings.HackSpeedMultiplier; set { _settings.HackSpeedMultiplier = value; OnPropertyChanged(); }
        }
        #endregion

        #region NPCs
        public bool EnableDrones
        {
            get => _settings.EnableDrones; set { _settings.EnableDrones = value; OnPropertyChanged(); }
        }

        public bool EnableEncounters
        {
            get => _settings.EnableEncounters; set { _settings.EnableEncounters = value; OnPropertyChanged(); }
        }

        public bool EnableSpiders
        {
            get => _settings.EnableSpiders; set { _settings.EnableSpiders = value; OnPropertyChanged(); }
        }

        public bool EnableWolves
        {
            get => _settings.EnableWolfs; set { _settings.EnableWolfs = value; OnPropertyChanged(); }
        }

        public bool EnableCargoShips
        {
            get => _settings.CargoShipsEnabled; set { _settings.CargoShipsEnabled = value; OnPropertyChanged(); }
        }
        #endregion

        #region Environment
        public bool EnableSunRotation
        {
            get => _settings.EnableSunRotation; set { _settings.EnableSunRotation = value; OnPropertyChanged(); }
        }

        public bool EnableAirtightness
        {
            get => _settings.EnableOxygenPressurization; set { _settings.EnableOxygenPressurization = value; OnPropertyChanged(); }
        }

        public bool EnableOxygen
        {
            get => _settings.EnableOxygen; set { _settings.EnableOxygen = value; OnPropertyChanged(); }
        }

        public bool EnableDestructibleBlocks
        {
            get => _settings.DestructibleBlocks; set { _settings.DestructibleBlocks = value; OnPropertyChanged(); }
        }

        public bool EnableToolShake
        {
            get => _settings.EnableToolShake; set { _settings.EnableToolShake = value; OnPropertyChanged(); }
        }

        public bool EnableVoxelDestruction
        {
            get => _settings.EnableVoxelDestruction; set { _settings.EnableVoxelDestruction = value; OnPropertyChanged(); }
        }

        public List<string> EnvironmentHostilityValues => Enum.GetNames(typeof(MyEnvironmentHostilityEnum)).ToList();

        public string EnvironmentHostility
        {
            get => _settings.EnvironmentHostility.ToString(); set { Enum.TryParse(value, true, out _settings.EnvironmentHostility); OnPropertyChanged(); }
        }

        public bool EnableFlora
        {
            get => _settings.EnableFlora; set { _settings.EnableFlora = value; OnPropertyChanged(); }
        }
        #endregion

        public List<string> GameModeValues => Enum.GetNames(typeof(MyGameModeEnum)).ToList();

        public string GameMode
        {
            get => _settings.GameMode.ToString(); set { Enum.TryParse(value, true, out _settings.GameMode); OnPropertyChanged(); }
        }

        public bool EnableAutoHealing
        {
            get => _settings.AutoHealing; set { _settings.AutoHealing = value; OnPropertyChanged(); }
        }

        public bool EnableCopyPaste
        {
            get => _settings.EnableCopyPaste; set { _settings.EnableCopyPaste = value; OnPropertyChanged(); }
        }

        public bool ShowPlayerNamesOnHud
        {
            get => _settings.ShowPlayerNamesOnHud; set { _settings.ShowPlayerNamesOnHud = value; OnPropertyChanged(); }
        }

        public bool EnableThirdPerson
        {
            get => _settings.Enable3rdPersonView; set { _settings.Enable3rdPersonView = value; OnPropertyChanged(); }
        }

        public bool EnableSpectator
        {
            get => _settings.EnableSpectator; set { _settings.EnableSpectator = value; OnPropertyChanged(); }
        }

        public bool SpawnWithTools
        {
            get => _settings.SpawnWithTools; set { _settings.SpawnWithTools = value; OnPropertyChanged(); }
        }

        public bool EnableConvertToStation
        {
            get => _settings.EnableConvertToStation; set { _settings.EnableConvertToStation = value; OnPropertyChanged(); }
        }

        public bool EnableJetpack
        {
            get => _settings.EnableJetpack; set { _settings.EnableJetpack = value; OnPropertyChanged(); }
        }

        public bool EnableRemoteOwnerRemoval
        {
            get => _settings.EnableRemoteBlockRemoval; set { _settings.EnableRemoteBlockRemoval = value; OnPropertyChanged(); }
        }

        public bool EnableRespawnShips
        {
            get => _settings.EnableRespawnShips; set { _settings.EnableRespawnShips = value; OnPropertyChanged(); }
        }

        public bool EnableScripterRole
        {
            get => _settings.EnableScripterRole; set { _settings.EnableScripterRole = value; OnPropertyChanged(); }
        }

        public bool EnableRealisticSound
        {
            get => _settings.RealisticSound; set { _settings.RealisticSound = value; OnPropertyChanged(); }
        }

        public bool ResetOwnership
        {
            get => _settings.ResetOwnership; set { _settings.ResetOwnership = value; OnPropertyChanged(); }
        }

        public bool DeleteRespawnShips
        {
            get => _settings.RespawnShipDelete; set { _settings.RespawnShipDelete = value; OnPropertyChanged(); }
        }

        public bool EnableThrusterDamage
        {
            get => _settings.ThrusterDamage; set { _settings.ThrusterDamage = value; OnPropertyChanged(); }
        }

        public bool EnableWeapons
        {
            get => _settings.WeaponsEnabled; set { _settings.WeaponsEnabled = value; OnPropertyChanged(); }
        }

        public bool EnableIngameScripts
        {
            get => _settings.EnableIngameScripts; set { _settings.EnableIngameScripts = value; OnPropertyChanged(); }
        }

        public uint AutosaveInterval
        {
            get => _settings.AutoSaveInMinutes; set { _settings.AutoSaveInMinutes = value; OnPropertyChanged(); }
        }

        public int FloraDensity
        {
            get => _settings.FloraDensity; set { _settings.FloraDensity = value; OnPropertyChanged(); }
        }

        public float FloraDensityMultiplier
        {
            get => _settings.FloraDensityMultiplier; set { _settings.FloraDensityMultiplier = value; OnPropertyChanged(); }
        }

        public short MaxBackupSaves
        {
            get => _settings.MaxBackupSaves; set { _settings.MaxBackupSaves = value; OnPropertyChanged(); }
        }

        public int MaxBlocksPerPlayer
        {
            get => _settings.MaxBlocksPerPlayer; set { _settings.MaxBlocksPerPlayer = value; OnPropertyChanged(); }
        }

        public short MaxFloatingObjects
        {
            get => _settings.MaxFloatingObjects; set { _settings.MaxFloatingObjects = value; OnPropertyChanged(); }
        }

        public int MaxGridSize
        {
            get => _settings.MaxGridSize; set { _settings.MaxGridSize = value; OnPropertyChanged(); }
        }

        public short MaxPlayers
        {
            get => _settings.MaxPlayers; set { _settings.MaxPlayers = value; OnPropertyChanged(); }
        }

        public int PhysicsIterations
        {
            get => _settings.PhysicsIterations; set { _settings.PhysicsIterations = value; OnPropertyChanged(); }
        }

        public float SpawnTimeMultiplier
        {
            get => _settings.SpawnShipTimeMultiplier; set { _settings.SpawnShipTimeMultiplier = value; OnPropertyChanged(); }
        }

        public float SunRotationInterval
        {
            get => _settings.SunRotationIntervalMinutes; set { _settings.SunRotationIntervalMinutes = value; OnPropertyChanged(); }
        }

        public int ViewDistance
        {
            get => _settings.ViewDistance; set { _settings.ViewDistance = value; OnPropertyChanged(); }
        }

        public int WorldSize
        {
            get => _settings.WorldSizeKm; set { _settings.WorldSizeKm = value; OnPropertyChanged(); }
        }

        public static implicit operator MyObjectBuilder_SessionSettings(SessionSettingsViewModel viewModel)
        {
            viewModel._settings.BlockTypeLimits.Dictionary.Clear();
            foreach (var limit in viewModel.BlockLimits)
                viewModel._settings.BlockTypeLimits.Dictionary.Add(limit.BlockType, limit.Limit);
            return viewModel._settings;
        }
    }
}
