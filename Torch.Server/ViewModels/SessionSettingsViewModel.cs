using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Toolkit.Collections;
using Torch.Collections;
using VRage.Game;
using VRage.Library.Utils;

namespace Torch.Server.ViewModels
{
    /// <summary>
    /// View model for <see cref="MyObjectBuilder_SessionSettings"/>
    /// </summary>
    public class SessionSettingsViewModel : ViewModel
    {
        private MyObjectBuilder_SessionSettings _settings;

        /// <summary>
        /// Creates a new view model with a new <see cref="MyObjectBuilder_SessionSettings"/> object.
        /// </summary>
        public SessionSettingsViewModel() : this(new MyObjectBuilder_SessionSettings())
        {

        }

        /// <summary>
        /// Creates a view model using an existing <see cref="MyObjectBuilder_SessionSettings"/> object.
        /// </summary>
        public SessionSettingsViewModel(MyObjectBuilder_SessionSettings settings)
        {
            _settings = settings;
            foreach (var limit in settings.BlockTypeLimits.Dictionary)
                BlockLimits.Add(new BlockLimitViewModel(this, limit.Key, limit.Value));
        }

        public MtObservableList<BlockLimitViewModel> BlockLimits { get; } = new MtObservableList<BlockLimitViewModel>();

        #region Multipliers

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.InventorySizeMultiplier"/>
        public float InventorySizeMultiplier
        {
            get => _settings.InventorySizeMultiplier; set { _settings.InventorySizeMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.RefinerySpeedMultiplier"/>
        public float RefinerySpeedMultiplier
        {
            get => _settings.RefinerySpeedMultiplier; set { _settings.RefinerySpeedMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.AssemblerEfficiencyMultiplier"/>
        public float AssemblerEfficiencyMultiplier
        {
            get => _settings.AssemblerEfficiencyMultiplier; set { _settings.AssemblerEfficiencyMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.AssemblerSpeedMultiplier"/>
        public float AssemblerSpeedMultiplier
        {
            get => _settings.AssemblerSpeedMultiplier; set { _settings.AssemblerSpeedMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.GrinderSpeedMultiplier"/>
        public float GrinderSpeedMultiplier
        {
            get => _settings.GrinderSpeedMultiplier; set { _settings.GrinderSpeedMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.HackSpeedMultiplier"/>
        public float HackSpeedMultiplier
        {
            get => _settings.HackSpeedMultiplier; set { _settings.HackSpeedMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.WelderSpeedMultiplier"/>
        public float WelderSpeedMultiplier
        {
            get => _settings.WelderSpeedMultiplier; set { _settings.WelderSpeedMultiplier = value; OnPropertyChanged(); }
        }
        #endregion

        #region NPCs

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableDrones"/>
        public bool EnableDrones
        {
            get => _settings.EnableDrones; set { _settings.EnableDrones = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableEncounters"/>
        public bool EnableEncounters
        {
            get => _settings.EnableEncounters; set { _settings.EnableEncounters = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableSpiders"/>
        public bool EnableSpiders
        {
            get => _settings.EnableSpiders; set { _settings.EnableSpiders = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableWolfs"/>
        public bool EnableWolves
        {
            get => _settings.EnableWolfs; set { _settings.EnableWolfs = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.CargoShipsEnabled"/>
        public bool EnableCargoShips
        {
            get => _settings.CargoShipsEnabled; set { _settings.CargoShipsEnabled = value; OnPropertyChanged(); }
        }
        #endregion

        #region Environment

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableSunRotation"/>
        public bool EnableSunRotation
        {
            get => _settings.EnableSunRotation; set { _settings.EnableSunRotation = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableOxygenPressurization"/>
        public bool EnableAirtightness
        {
            get => _settings.EnableOxygenPressurization; set { _settings.EnableOxygenPressurization = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableOxygen"/>
        public bool EnableOxygen
        {
            get => _settings.EnableOxygen; set { _settings.EnableOxygen = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.DestructibleBlocks"/>
        public bool EnableDestructibleBlocks
        {
            get => _settings.DestructibleBlocks; set { _settings.DestructibleBlocks = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableToolShake"/>
        public bool EnableToolShake
        {
            get => _settings.EnableToolShake; set { _settings.EnableToolShake = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableVoxelDestruction"/>
        public bool EnableVoxelDestruction
        {
            get => _settings.EnableVoxelDestruction; set { _settings.EnableVoxelDestruction = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List used to populate the environment hostility combo box.
        /// </summary>
        public List<string> EnvironmentHostilityValues { get; } = Enum.GetNames(typeof(MyEnvironmentHostilityEnum)).ToList();

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnvironmentHostility"/>
        public string EnvironmentHostility
        {
            get => _settings.EnvironmentHostility.ToString(); set { Enum.TryParse(value, true, out _settings.EnvironmentHostility); OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableFlora"/>
        public bool EnableFlora
        {
            get => _settings.EnableFlora; set { _settings.EnableFlora = value; OnPropertyChanged(); }
        }
        #endregion

        /// <summary>
        /// List used to populate the game mode combobox.
        /// </summary>
        public List<string> GameModeValues { get; } = Enum.GetNames(typeof(MyGameModeEnum)).ToList();

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.GameMode"/>
        public string GameMode
        {
            get => _settings.GameMode.ToString(); set { Enum.TryParse(value, true, out _settings.GameMode); OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.AutoHealing"/>
        public bool EnableAutoHealing
        {
            get => _settings.AutoHealing; set { _settings.AutoHealing = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableCopyPaste"/>
        public bool EnableCopyPaste
        {
            get => _settings.EnableCopyPaste; set { _settings.EnableCopyPaste = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ShowPlayerNamesOnHud"/>
        public bool ShowPlayerNamesOnHud
        {
            get => _settings.ShowPlayerNamesOnHud; set { _settings.ShowPlayerNamesOnHud = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.Enable3rdPersonView"/>
        public bool EnableThirdPerson
        {
            get => _settings.Enable3rdPersonView; set { _settings.Enable3rdPersonView = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableSpectator"/>
        public bool EnableSpectator
        {
            get => _settings.EnableSpectator; set { _settings.EnableSpectator = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.SpawnWithTools"/>
        public bool SpawnWithTools
        {
            get => _settings.SpawnWithTools; set { _settings.SpawnWithTools = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableConvertToStation"/>
        public bool EnableConvertToStation
        {
            get => _settings.EnableConvertToStation; set { _settings.EnableConvertToStation = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableJetpack"/>
        public bool EnableJetpack
        {
            get => _settings.EnableJetpack; set { _settings.EnableJetpack = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableRemoteBlockRemoval"/>
        public bool EnableRemoteOwnerRemoval
        {
            get => _settings.EnableRemoteBlockRemoval; set { _settings.EnableRemoteBlockRemoval = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableRespawnShips"/>
        public bool EnableRespawnShips
        {
            get => _settings.EnableRespawnShips; set { _settings.EnableRespawnShips = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableScripterRole"/>
        public bool EnableScripterRole
        {
            get => _settings.EnableScripterRole; set { _settings.EnableScripterRole = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.RealisticSound"/>
        public bool EnableRealisticSound
        {
            get => _settings.RealisticSound; set { _settings.RealisticSound = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ResetOwnership"/>
        public bool ResetOwnership
        {
            get => _settings.ResetOwnership; set { _settings.ResetOwnership = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.RespawnShipDelete"/>
        public bool DeleteRespawnShips
        {
            get => _settings.RespawnShipDelete; set { _settings.RespawnShipDelete = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ThrusterDamage"/>
        public bool EnableThrusterDamage
        {
            get => _settings.ThrusterDamage; set { _settings.ThrusterDamage = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.WeaponsEnabled"/>
        public bool EnableWeapons
        {
            get => _settings.WeaponsEnabled; set { _settings.WeaponsEnabled = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.EnableIngameScripts"/>
        public bool EnableIngameScripts
        {
            get => _settings.EnableIngameScripts; set { _settings.EnableIngameScripts = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.AutoSaveInMinutes"/>
        public uint AutosaveInterval
        {
            get => _settings.AutoSaveInMinutes; set { _settings.AutoSaveInMinutes = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.FloraDensity"/>
        public int FloraDensity
        {
            get => _settings.FloraDensity; set { _settings.FloraDensity = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.FloraDensityMultiplier"/>
        public float FloraDensityMultiplier
        {
            get => _settings.FloraDensityMultiplier; set { _settings.FloraDensityMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.MaxBackupSaves"/>
        public short MaxBackupSaves
        {
            get => _settings.MaxBackupSaves; set { _settings.MaxBackupSaves = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.MaxBlocksPerPlayer"/>
        public int MaxBlocksPerPlayer
        {
            get => _settings.MaxBlocksPerPlayer; set { _settings.MaxBlocksPerPlayer = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.MaxFloatingObjects"/>
        public short MaxFloatingObjects
        {
            get => _settings.MaxFloatingObjects; set { _settings.MaxFloatingObjects = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.MaxGridSize"/>
        public int MaxGridSize
        {
            get => _settings.MaxGridSize; set { _settings.MaxGridSize = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.MaxPlayers"/>
        public short MaxPlayers
        {
            get => _settings.MaxPlayers; set { _settings.MaxPlayers = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.PhysicsIterations"/>
        public int PhysicsIterations
        {
            get => _settings.PhysicsIterations; set { _settings.PhysicsIterations = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.SpawnShipTimeMultiplier"/>
        public float SpawnTimeMultiplier
        {
            get => _settings.SpawnShipTimeMultiplier; set { _settings.SpawnShipTimeMultiplier = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.SunRotationIntervalMinutes"/>
        public float SunRotationInterval
        {
            get => _settings.SunRotationIntervalMinutes; set { _settings.SunRotationIntervalMinutes = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ViewDistance"/>
        public int ViewDistance
        {
            get => _settings.ViewDistance; set { _settings.ViewDistance = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.WorldSizeKm"/>
        public int WorldSize
        {
            get => _settings.WorldSizeKm; set { _settings.WorldSizeKm = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ProceduralDensity"/>
        public float ProceduralDensity
        {
            get => _settings.ProceduralDensity; set { _settings.ProceduralDensity = value; OnPropertyChanged(); }
        }

        /// <inheritdoc cref="MyObjectBuilder_SessionSettings.ProceduralSeed"/>
        public int ProceduralSeed
        {
            get => _settings.ProceduralSeed;
            set { _settings.ProceduralSeed = value; OnPropertyChanged(); }
        }

        /// <summary />
        public static implicit operator MyObjectBuilder_SessionSettings(SessionSettingsViewModel viewModel)
        {
            viewModel._settings.BlockTypeLimits.Dictionary.Clear();
            foreach (var limit in viewModel.BlockLimits)
                viewModel._settings.BlockTypeLimits.Dictionary.Add(limit.BlockType, limit.Limit);
            return viewModel._settings;
        }
    }
}
