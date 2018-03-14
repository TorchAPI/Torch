// This file is generated automatically! Any changes will be overwritten.

using System;
using System.Collections.Generic;
using System.Linq;
using Torch;
using Torch.Collections;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Serialization;

namespace Torch.Server.ViewModels
{
	public class SessionSettingsViewModel : ViewModel
	{
		private MyObjectBuilder_SessionSettings _settings;
	/// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.GameMode" />
        public string GameMode { get => _settings.GameMode.ToString(); set { Enum.TryParse(value, true, out VRage.Library.Utils.MyGameModeEnum parsedVal); SetValue(ref _settings.GameMode, parsedVal); } }
        public List<string> GameModeValues { get; } = new List<string> {"Creative", "Survival"};

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.InventorySizeMultiplier" />
        public System.Single InventorySizeMultiplier { get => _settings.InventorySizeMultiplier; set => SetValue(ref _settings.InventorySizeMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.AssemblerSpeedMultiplier" />
        public System.Single AssemblerSpeedMultiplier { get => _settings.AssemblerSpeedMultiplier; set => SetValue(ref _settings.AssemblerSpeedMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.AssemblerEfficiencyMultiplier" />
        public System.Single AssemblerEfficiencyMultiplier { get => _settings.AssemblerEfficiencyMultiplier; set => SetValue(ref _settings.AssemblerEfficiencyMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.RefinerySpeedMultiplier" />
        public System.Single RefinerySpeedMultiplier { get => _settings.RefinerySpeedMultiplier; set => SetValue(ref _settings.RefinerySpeedMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.OnlineMode" />
        public string OnlineMode { get => _settings.OnlineMode.ToString(); set { Enum.TryParse(value, true, out VRage.Game.MyOnlineModeEnum parsedVal); SetValue(ref _settings.OnlineMode, parsedVal); } }
        public List<string> OnlineModeValues { get; } = new List<string> {"OFFLINE", "PUBLIC", "FRIENDS", "PRIVATE"};

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxPlayers" />
        public System.Int16 MaxPlayers { get => _settings.MaxPlayers; set => SetValue(ref _settings.MaxPlayers, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxFloatingObjects" />
        public System.Int16 MaxFloatingObjects { get => _settings.MaxFloatingObjects; set => SetValue(ref _settings.MaxFloatingObjects, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxBackupSaves" />
        public System.Int16 MaxBackupSaves { get => _settings.MaxBackupSaves; set => SetValue(ref _settings.MaxBackupSaves, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxGridSize" />
        public System.Int32 MaxGridSize { get => _settings.MaxGridSize; set => SetValue(ref _settings.MaxGridSize, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxBlocksPerPlayer" />
        public System.Int32 MaxBlocksPerPlayer { get => _settings.MaxBlocksPerPlayer; set => SetValue(ref _settings.MaxBlocksPerPlayer, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableBlockLimits" />
        public System.Boolean EnableBlockLimits { get => _settings.EnableBlockLimits; set => SetValue(ref _settings.EnableBlockLimits, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRemoteBlockRemoval" />
        public System.Boolean EnableRemoteBlockRemoval { get => _settings.EnableRemoteBlockRemoval; set => SetValue(ref _settings.EnableRemoteBlockRemoval, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnvironmentHostility" />
        public string EnvironmentHostility { get => _settings.EnvironmentHostility.ToString(); set { Enum.TryParse(value, true, out VRage.Game.MyEnvironmentHostilityEnum parsedVal); SetValue(ref _settings.EnvironmentHostility, parsedVal); } }
        public List<string> EnvironmentHostilityValues { get; } = new List<string> {"SAFE", "NORMAL", "CATACLYSM", "CATACLYSM_UNREAL"};

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.AutoHealing" />
        public System.Boolean AutoHealing { get => _settings.AutoHealing; set => SetValue(ref _settings.AutoHealing, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableCopyPaste" />
        public System.Boolean EnableCopyPaste { get => _settings.EnableCopyPaste; set => SetValue(ref _settings.EnableCopyPaste, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.WeaponsEnabled" />
        public System.Boolean WeaponsEnabled { get => _settings.WeaponsEnabled; set => SetValue(ref _settings.WeaponsEnabled, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ShowPlayerNamesOnHud" />
        public System.Boolean ShowPlayerNamesOnHud { get => _settings.ShowPlayerNamesOnHud; set => SetValue(ref _settings.ShowPlayerNamesOnHud, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ThrusterDamage" />
        public System.Boolean ThrusterDamage { get => _settings.ThrusterDamage; set => SetValue(ref _settings.ThrusterDamage, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.CargoShipsEnabled" />
        public System.Boolean CargoShipsEnabled { get => _settings.CargoShipsEnabled; set => SetValue(ref _settings.CargoShipsEnabled, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSpectator" />
        public System.Boolean EnableSpectator { get => _settings.EnableSpectator; set => SetValue(ref _settings.EnableSpectator, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.WorldSizeKm" />
        public System.Int32 WorldSizeKm { get => _settings.WorldSizeKm; set => SetValue(ref _settings.WorldSizeKm, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.RespawnShipDelete" />
        public System.Boolean RespawnShipDelete { get => _settings.RespawnShipDelete; set => SetValue(ref _settings.RespawnShipDelete, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ResetOwnership" />
        public System.Boolean ResetOwnership { get => _settings.ResetOwnership; set => SetValue(ref _settings.ResetOwnership, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.WelderSpeedMultiplier" />
        public System.Single WelderSpeedMultiplier { get => _settings.WelderSpeedMultiplier; set => SetValue(ref _settings.WelderSpeedMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.GrinderSpeedMultiplier" />
        public System.Single GrinderSpeedMultiplier { get => _settings.GrinderSpeedMultiplier; set => SetValue(ref _settings.GrinderSpeedMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.RealisticSound" />
        public System.Boolean RealisticSound { get => _settings.RealisticSound; set => SetValue(ref _settings.RealisticSound, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.HackSpeedMultiplier" />
        public System.Single HackSpeedMultiplier { get => _settings.HackSpeedMultiplier; set => SetValue(ref _settings.HackSpeedMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.PermanentDeath" />
        public System.Nullable<System.Boolean> PermanentDeath { get => _settings.PermanentDeath; set => SetValue(ref _settings.PermanentDeath, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.AutoSaveInMinutes" />
        public System.UInt32 AutoSaveInMinutes { get => _settings.AutoSaveInMinutes; set => SetValue(ref _settings.AutoSaveInMinutes, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSaving" />
        public System.Boolean EnableSaving { get => _settings.EnableSaving; set => SetValue(ref _settings.EnableSaving, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRespawnScreen" />
        public System.Boolean EnableRespawnScreen { get => _settings.EnableRespawnScreen; set => SetValue(ref _settings.EnableRespawnScreen, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.InfiniteAmmo" />
        public System.Boolean InfiniteAmmo { get => _settings.InfiniteAmmo; set => SetValue(ref _settings.InfiniteAmmo, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableContainerDrops" />
        public System.Boolean EnableContainerDrops { get => _settings.EnableContainerDrops; set => SetValue(ref _settings.EnableContainerDrops, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.SpawnShipTimeMultiplier" />
        public System.Single SpawnShipTimeMultiplier { get => _settings.SpawnShipTimeMultiplier; set => SetValue(ref _settings.SpawnShipTimeMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ProceduralDensity" />
        public System.Single ProceduralDensity { get => _settings.ProceduralDensity; set => SetValue(ref _settings.ProceduralDensity, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ProceduralSeed" />
        public System.Int32 ProceduralSeed { get => _settings.ProceduralSeed; set => SetValue(ref _settings.ProceduralSeed, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.DestructibleBlocks" />
        public System.Boolean DestructibleBlocks { get => _settings.DestructibleBlocks; set => SetValue(ref _settings.DestructibleBlocks, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableIngameScripts" />
        public System.Boolean EnableIngameScripts { get => _settings.EnableIngameScripts; set => SetValue(ref _settings.EnableIngameScripts, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ViewDistance" />
        public System.Int32 ViewDistance { get => _settings.ViewDistance; set => SetValue(ref _settings.ViewDistance, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.FloraDensity" />
        public System.Int32 FloraDensity { get => _settings.FloraDensity; set => SetValue(ref _settings.FloraDensity, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableToolShake" />
        public System.Boolean EnableToolShake { get => _settings.EnableToolShake; set => SetValue(ref _settings.EnableToolShake, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.VoxelGeneratorVersion" />
        public System.Int32 VoxelGeneratorVersion { get => _settings.VoxelGeneratorVersion; set => SetValue(ref _settings.VoxelGeneratorVersion, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableOxygen" />
        public System.Boolean EnableOxygen { get => _settings.EnableOxygen; set => SetValue(ref _settings.EnableOxygen, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableOxygenPressurization" />
        public System.Boolean EnableOxygenPressurization { get => _settings.EnableOxygenPressurization; set => SetValue(ref _settings.EnableOxygenPressurization, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.Enable3rdPersonView" />
        public System.Boolean Enable3rdPersonView { get => _settings.Enable3rdPersonView; set => SetValue(ref _settings.Enable3rdPersonView, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableEncounters" />
        public System.Boolean EnableEncounters { get => _settings.EnableEncounters; set => SetValue(ref _settings.EnableEncounters, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableFlora" />
        public System.Boolean EnableFlora { get => _settings.EnableFlora; set => SetValue(ref _settings.EnableFlora, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableConvertToStation" />
        public System.Boolean EnableConvertToStation { get => _settings.EnableConvertToStation; set => SetValue(ref _settings.EnableConvertToStation, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.StationVoxelSupport" />
        public System.Boolean StationVoxelSupport { get => _settings.StationVoxelSupport; set => SetValue(ref _settings.StationVoxelSupport, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSunRotation" />
        public System.Boolean EnableSunRotation { get => _settings.EnableSunRotation; set => SetValue(ref _settings.EnableSunRotation, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRespawnShips" />
        public System.Boolean EnableRespawnShips { get => _settings.EnableRespawnShips; set => SetValue(ref _settings.EnableRespawnShips, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.ScenarioEditMode" />
        public System.Boolean ScenarioEditMode { get => _settings.ScenarioEditMode; set => SetValue(ref _settings.ScenarioEditMode, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.Scenario" />
        public System.Boolean Scenario { get => _settings.Scenario; set => SetValue(ref _settings.Scenario, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.CanJoinRunning" />
        public System.Boolean CanJoinRunning { get => _settings.CanJoinRunning; set => SetValue(ref _settings.CanJoinRunning, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.PhysicsIterations" />
        public System.Int32 PhysicsIterations { get => _settings.PhysicsIterations; set => SetValue(ref _settings.PhysicsIterations, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.SunRotationIntervalMinutes" />
        public System.Single SunRotationIntervalMinutes { get => _settings.SunRotationIntervalMinutes; set => SetValue(ref _settings.SunRotationIntervalMinutes, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableJetpack" />
        public System.Boolean EnableJetpack { get => _settings.EnableJetpack; set => SetValue(ref _settings.EnableJetpack, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.SpawnWithTools" />
        public System.Boolean SpawnWithTools { get => _settings.SpawnWithTools; set => SetValue(ref _settings.SpawnWithTools, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.StartInRespawnScreen" />
        public System.Boolean StartInRespawnScreen { get => _settings.StartInRespawnScreen; set => SetValue(ref _settings.StartInRespawnScreen, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableVoxelDestruction" />
        public System.Boolean EnableVoxelDestruction { get => _settings.EnableVoxelDestruction; set => SetValue(ref _settings.EnableVoxelDestruction, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxDrones" />
        public System.Int32 MaxDrones { get => _settings.MaxDrones; set => SetValue(ref _settings.MaxDrones, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableDrones" />
        public System.Boolean EnableDrones { get => _settings.EnableDrones; set => SetValue(ref _settings.EnableDrones, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableWolfs" />
        public System.Boolean EnableWolfs { get => _settings.EnableWolfs; set => SetValue(ref _settings.EnableWolfs, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSpiders" />
        public System.Boolean EnableSpiders { get => _settings.EnableSpiders; set => SetValue(ref _settings.EnableSpiders, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.FloraDensityMultiplier" />
        public System.Single FloraDensityMultiplier { get => _settings.FloraDensityMultiplier; set => SetValue(ref _settings.FloraDensityMultiplier, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableStructuralSimulation" />
        public System.Boolean EnableStructuralSimulation { get => _settings.EnableStructuralSimulation; set => SetValue(ref _settings.EnableStructuralSimulation, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxActiveFracturePieces" />
        public System.Int32 MaxActiveFracturePieces { get => _settings.MaxActiveFracturePieces; set => SetValue(ref _settings.MaxActiveFracturePieces, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.BlockTypeLimits" />
        public VRage.Serialization.SerializableDictionary<System.String, System.Int16> BlockTypeLimits { get => _settings.BlockTypeLimits; set => SetValue(ref _settings.BlockTypeLimits, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableScripterRole" />
        public System.Boolean EnableScripterRole { get => _settings.EnableScripterRole; set => SetValue(ref _settings.EnableScripterRole, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MinDropContainerRespawnTime" />
        public System.Int32 MinDropContainerRespawnTime { get => _settings.MinDropContainerRespawnTime; set => SetValue(ref _settings.MinDropContainerRespawnTime, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxDropContainerRespawnTime" />
        public System.Int32 MaxDropContainerRespawnTime { get => _settings.MaxDropContainerRespawnTime; set => SetValue(ref _settings.MaxDropContainerRespawnTime, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableTurretsFriendlyFire" />
        public System.Boolean EnableTurretsFriendlyFire { get => _settings.EnableTurretsFriendlyFire; set => SetValue(ref _settings.EnableTurretsFriendlyFire, value); }

        /// <inheritdoc cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSubgridDamage" />
        public System.Boolean EnableSubgridDamage { get => _settings.EnableSubgridDamage; set => SetValue(ref _settings.EnableSubgridDamage, value); }


		public SessionSettingsViewModel(MyObjectBuilder_SessionSettings settings)
		{
			_settings = settings;
		}

		public static implicit operator MyObjectBuilder_SessionSettings(SessionSettingsViewModel viewModel)
		{
			return viewModel._settings;
		}
	}
}
