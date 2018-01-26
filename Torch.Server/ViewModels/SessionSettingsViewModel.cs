// This file is generated automatically! Any changes will be overwritten.

using System;
using System.Collections.Generic;
using System.Linq;
using Torch;
using Torch.Collections;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Torch.Server.ViewModels
{
	public class SessionSettingsViewModel : ViewModel
	{
		private MyObjectBuilder_SessionSettings _settings;
	/// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.GameMode" />
        [Display(Name = "Game mode")]
        public VRage.Library.Utils.MyGameModeEnum GameMode { get => _settings.GameMode; set => SetValue(ref _settings.GameMode, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.InventorySizeMultiplier" />
        [Display(Name = "Inventory size multiplier")]
        public System.Single InventorySizeMultiplier { get => _settings.InventorySizeMultiplier; set => SetValue(ref _settings.InventorySizeMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.AssemblerSpeedMultiplier" />
        [Display(Name = "Assembler speed multiplier")]
        public System.Single AssemblerSpeedMultiplier { get => _settings.AssemblerSpeedMultiplier; set => SetValue(ref _settings.AssemblerSpeedMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.AssemblerEfficiencyMultiplier" />
        [Display(Name = "Assembler efficiency multiplier")]
        public System.Single AssemblerEfficiencyMultiplier { get => _settings.AssemblerEfficiencyMultiplier; set => SetValue(ref _settings.AssemblerEfficiencyMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.RefinerySpeedMultiplier" />
        [Display(Name = "Refinery speed multiplier")]
        public System.Single RefinerySpeedMultiplier { get => _settings.RefinerySpeedMultiplier; set => SetValue(ref _settings.RefinerySpeedMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.OnlineMode" />
        [Display(Name = "OnlineMode")]
        public VRage.Game.MyOnlineModeEnum OnlineMode { get => _settings.OnlineMode; set => SetValue(ref _settings.OnlineMode, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxPlayers" />
        [Display(Name = "Max players")]
        public System.Int16 MaxPlayers { get => _settings.MaxPlayers; set => SetValue(ref _settings.MaxPlayers, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxFloatingObjects" />
        [Display(Name = "Max floating objects")]
        public System.Int16 MaxFloatingObjects { get => _settings.MaxFloatingObjects; set => SetValue(ref _settings.MaxFloatingObjects, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxBackupSaves" />
        [Display(Name = "Max Backup Saves")]
        public System.Int16 MaxBackupSaves { get => _settings.MaxBackupSaves; set => SetValue(ref _settings.MaxBackupSaves, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxGridSize" />
        [Display(Name = "Max grid size")]
        public System.Int32 MaxGridSize { get => _settings.MaxGridSize; set => SetValue(ref _settings.MaxGridSize, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxBlocksPerPlayer" />
        [Display(Name = "Max blocks per player")]
        public System.Int32 MaxBlocksPerPlayer { get => _settings.MaxBlocksPerPlayer; set => SetValue(ref _settings.MaxBlocksPerPlayer, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableBlockLimits" />
        [Display(Name = "Enable block limits")]
        public System.Boolean EnableBlockLimits { get => _settings.EnableBlockLimits; set => SetValue(ref _settings.EnableBlockLimits, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRemoteBlockRemoval" />
        [Display(Name = "Enable remote removal of owned blocks")]
        public System.Boolean EnableRemoteBlockRemoval { get => _settings.EnableRemoteBlockRemoval; set => SetValue(ref _settings.EnableRemoteBlockRemoval, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnvironmentHostility" />
        [Display(Name = "Environment hostility")]
        public VRage.Game.MyEnvironmentHostilityEnum EnvironmentHostility { get => _settings.EnvironmentHostility; set => SetValue(ref _settings.EnvironmentHostility, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.AutoHealing" />
        [Display(Name = "Auto healing")]
        public System.Boolean AutoHealing { get => _settings.AutoHealing; set => SetValue(ref _settings.AutoHealing, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableCopyPaste" />
        [Display(Name = "Enable Copy&Paste")]
        public System.Boolean EnableCopyPaste { get => _settings.EnableCopyPaste; set => SetValue(ref _settings.EnableCopyPaste, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.WeaponsEnabled" />
        [Display(Name = "Weapons enabled")]
        public System.Boolean WeaponsEnabled { get => _settings.WeaponsEnabled; set => SetValue(ref _settings.WeaponsEnabled, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ShowPlayerNamesOnHud" />
        [Display(Name = "Show player names on HUD")]
        public System.Boolean ShowPlayerNamesOnHud { get => _settings.ShowPlayerNamesOnHud; set => SetValue(ref _settings.ShowPlayerNamesOnHud, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ThrusterDamage" />
        [Display(Name = "Thruster damage")]
        public System.Boolean ThrusterDamage { get => _settings.ThrusterDamage; set => SetValue(ref _settings.ThrusterDamage, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.CargoShipsEnabled" />
        [Display(Name = "Cargo ships enabled")]
        public System.Boolean CargoShipsEnabled { get => _settings.CargoShipsEnabled; set => SetValue(ref _settings.CargoShipsEnabled, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSpectator" />
        [Display(Name = "Enable spectator")]
        public System.Boolean EnableSpectator { get => _settings.EnableSpectator; set => SetValue(ref _settings.EnableSpectator, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.WorldSizeKm" />
        [Display(Name = "World size in Km")]
        public System.Int32 WorldSizeKm { get => _settings.WorldSizeKm; set => SetValue(ref _settings.WorldSizeKm, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.RespawnShipDelete" />
        [Display(Name = "Respawn ship delete")]
        public System.Boolean RespawnShipDelete { get => _settings.RespawnShipDelete; set => SetValue(ref _settings.RespawnShipDelete, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ResetOwnership" />
        [Display(Name = "Reset ownership")]
        public System.Boolean ResetOwnership { get => _settings.ResetOwnership; set => SetValue(ref _settings.ResetOwnership, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.WelderSpeedMultiplier" />
        [Display(Name = "Welder speed multiplier")]
        public System.Single WelderSpeedMultiplier { get => _settings.WelderSpeedMultiplier; set => SetValue(ref _settings.WelderSpeedMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.GrinderSpeedMultiplier" />
        [Display(Name = "Grinder speed multiplier")]
        public System.Single GrinderSpeedMultiplier { get => _settings.GrinderSpeedMultiplier; set => SetValue(ref _settings.GrinderSpeedMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.RealisticSound" />
        [Display(Name = "Realistic sound")]
        public System.Boolean RealisticSound { get => _settings.RealisticSound; set => SetValue(ref _settings.RealisticSound, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.HackSpeedMultiplier" />
        [Display(Name = "Hack speed multiplier")]
        public System.Single HackSpeedMultiplier { get => _settings.HackSpeedMultiplier; set => SetValue(ref _settings.HackSpeedMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.PermanentDeath" />
        [Display(Name = "Permanent death")]
        public System.Nullable<System.Boolean> PermanentDeath { get => _settings.PermanentDeath; set => SetValue(ref _settings.PermanentDeath, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.AutoSaveInMinutes" />
        [Display(Name = "AutoSave in minutes")]
        public System.UInt32 AutoSaveInMinutes { get => _settings.AutoSaveInMinutes; set => SetValue(ref _settings.AutoSaveInMinutes, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSaving" />
        [Display(Name = "Enable saving from menu")]
        public System.Boolean EnableSaving { get => _settings.EnableSaving; set => SetValue(ref _settings.EnableSaving, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRespawnScreen" />
        [Display(Name = "Enable respawn screen in the game")]
        public System.Boolean EnableRespawnScreen { get => _settings.EnableRespawnScreen; set => SetValue(ref _settings.EnableRespawnScreen, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.InfiniteAmmo" />
        [Display(Name = "Enable infinite ammunition in survival")]
        public System.Boolean InfiniteAmmo { get => _settings.InfiniteAmmo; set => SetValue(ref _settings.InfiniteAmmo, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableContainerDrops" />
        [Display(Name = "Enable drop containers")]
        public System.Boolean EnableContainerDrops { get => _settings.EnableContainerDrops; set => SetValue(ref _settings.EnableContainerDrops, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.SpawnShipTimeMultiplier" />
        [Display(Name = "Spawnship time multiplier")]
        public System.Single SpawnShipTimeMultiplier { get => _settings.SpawnShipTimeMultiplier; set => SetValue(ref _settings.SpawnShipTimeMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ProceduralDensity" />
        [Display(Name = "Procedural density")]
        public System.Single ProceduralDensity { get => _settings.ProceduralDensity; set => SetValue(ref _settings.ProceduralDensity, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ProceduralSeed" />
        [Display(Name = "Procedural seed")]
        public System.Int32 ProceduralSeed { get => _settings.ProceduralSeed; set => SetValue(ref _settings.ProceduralSeed, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.DestructibleBlocks" />
        [Display(Name = "Destructible blocks")]
        public System.Boolean DestructibleBlocks { get => _settings.DestructibleBlocks; set => SetValue(ref _settings.DestructibleBlocks, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableIngameScripts" />
        [Display(Name = "Enable ingame scripts")]
        public System.Boolean EnableIngameScripts { get => _settings.EnableIngameScripts; set => SetValue(ref _settings.EnableIngameScripts, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.ViewDistance" />
        [Display(Name = "View distance")]
        public System.Int32 ViewDistance { get => _settings.ViewDistance; set => SetValue(ref _settings.ViewDistance, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.FloraDensity" />
        [Display(Name = "Flora density")]
        public System.Int32 FloraDensity { get => _settings.FloraDensity; set => SetValue(ref _settings.FloraDensity, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableToolShake" />
        [Display(Name = "Enable tool shake")]
        public System.Boolean EnableToolShake { get => _settings.EnableToolShake; set => SetValue(ref _settings.EnableToolShake, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.VoxelGeneratorVersion" />
        [Display(Name = "Voxel generator version")]
        public System.Int32 VoxelGeneratorVersion { get => _settings.VoxelGeneratorVersion; set => SetValue(ref _settings.VoxelGeneratorVersion, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableOxygen" />
        [Display(Name = "Enable oxygen")]
        public System.Boolean EnableOxygen { get => _settings.EnableOxygen; set => SetValue(ref _settings.EnableOxygen, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableOxygenPressurization" />
        [Display(Name = "Enable airtightness")]
        public System.Boolean EnableOxygenPressurization { get => _settings.EnableOxygenPressurization; set => SetValue(ref _settings.EnableOxygenPressurization, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.Enable3rdPersonView" />
        [Display(Name = "Enable 3rd person view")]
        public System.Boolean Enable3rdPersonView { get => _settings.Enable3rdPersonView; set => SetValue(ref _settings.Enable3rdPersonView, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableEncounters" />
        [Display(Name = "Enable encounters")]
        public System.Boolean EnableEncounters { get => _settings.EnableEncounters; set => SetValue(ref _settings.EnableEncounters, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableFlora" />
        [Display(Name = "Enable flora")]
        public System.Boolean EnableFlora { get => _settings.EnableFlora; set => SetValue(ref _settings.EnableFlora, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableConvertToStation" />
        [Display(Name = "Enable convert to station")]
        public System.Boolean EnableConvertToStation { get => _settings.EnableConvertToStation; set => SetValue(ref _settings.EnableConvertToStation, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.StationVoxelSupport" />
        [Display(Name = "Enable station grid with voxel")]
        public System.Boolean StationVoxelSupport { get => _settings.StationVoxelSupport; set => SetValue(ref _settings.StationVoxelSupport, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSunRotation" />
        [Display(Name = "Enable sun rotation")]
        public System.Boolean EnableSunRotation { get => _settings.EnableSunRotation; set => SetValue(ref _settings.EnableSunRotation, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableRespawnShips" />
        [Display(Name = "Enable respawn ships / carts")]
        public System.Boolean EnableRespawnShips { get => _settings.EnableRespawnShips; set => SetValue(ref _settings.EnableRespawnShips, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.PhysicsIterations" />
        [Display(Name = "PhysicsIterations")]
        public System.Int32 PhysicsIterations { get => _settings.PhysicsIterations; set => SetValue(ref _settings.PhysicsIterations, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.SunRotationIntervalMinutes" />
        [Display(Name = "Sun rotation interval")]
        public System.Single SunRotationIntervalMinutes { get => _settings.SunRotationIntervalMinutes; set => SetValue(ref _settings.SunRotationIntervalMinutes, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableJetpack" />
        [Display(Name = "Enable jetpack")]
        public System.Boolean EnableJetpack { get => _settings.EnableJetpack; set => SetValue(ref _settings.EnableJetpack, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.SpawnWithTools" />
        [Display(Name = "Spawn with tools")]
        public System.Boolean SpawnWithTools { get => _settings.SpawnWithTools; set => SetValue(ref _settings.SpawnWithTools, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableVoxelDestruction" />
        [Display(Name = "Enable voxel destruction")]
        public System.Boolean EnableVoxelDestruction { get => _settings.EnableVoxelDestruction; set => SetValue(ref _settings.EnableVoxelDestruction, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableDrones" />
        [Display(Name = "Enable drones")]
        public System.Boolean EnableDrones { get => _settings.EnableDrones; set => SetValue(ref _settings.EnableDrones, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableWolfs" />
        [Display(Name = "Enable wolfs")]
        public System.Boolean EnableWolfs { get => _settings.EnableWolfs; set => SetValue(ref _settings.EnableWolfs, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSpiders" />
        [Display(Name = "Enable spiders")]
        public System.Boolean EnableSpiders { get => _settings.EnableSpiders; set => SetValue(ref _settings.EnableSpiders, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.FloraDensityMultiplier" />
        [Display(Name = "Flora density multiplier")]
        public System.Single FloraDensityMultiplier { get => _settings.FloraDensityMultiplier; set => SetValue(ref _settings.FloraDensityMultiplier, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.BlockTypeLimits" />
        [Display(Name = "Block type limits")]
        public VRage.Serialization.SerializableDictionary<System.String, System.Int16> BlockTypeLimits { get => _settings.BlockTypeLimits; set => SetValue(ref _settings.BlockTypeLimits, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableScripterRole" />
        [Display(Name = "Enable Scripter role")]
        public System.Boolean EnableScripterRole { get => _settings.EnableScripterRole; set => SetValue(ref _settings.EnableScripterRole, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MinDropContainerRespawnTime" />
        [Display(Name = "Min Drop Container Respawn Time")]
        public System.Int32 MinDropContainerRespawnTime { get => _settings.MinDropContainerRespawnTime; set => SetValue(ref _settings.MinDropContainerRespawnTime, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.MaxDropContainerRespawnTime" />
        [Display(Name = "Max Drop Container Respawn Time")]
        public System.Int32 MaxDropContainerRespawnTime { get => _settings.MaxDropContainerRespawnTime; set => SetValue(ref _settings.MaxDropContainerRespawnTime, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableTurretsFriendlyFire" />
        [Display(Name = "Enable Turrets Friendly Fire")]
        public System.Boolean EnableTurretsFriendlyFire { get => _settings.EnableTurretsFriendlyFire; set => SetValue(ref _settings.EnableTurretsFriendlyFire, value); }

        /// <see cref="VRage.Game.MyObjectBuilder_SessionSettings.EnableSubgridDamage" />
        [Display(Name = "Enable Sub-Grid damage")]
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
