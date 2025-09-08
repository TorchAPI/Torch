using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.Game;
using VRage.Library.Utils;
using VRageRender.Utils;
using static Sandbox.Game.Multiplayer.MyPlayerCollection;
using static VRage.Dedicated.RemoteAPI.MyDedicatedController;

namespace Torch.Server.ViewModels
{
    public class SessionSettingsViewModel : ViewModel
    {
        private MyObjectBuilder_SessionSettings _settings;

        // This view model could be changed where the settings are dynamically generated from the attributes on the members in MyObjectBuilder_SessionSettings,
        // this would prevent issues where new settings are forgotten, and would allow settings to be removed and added without an update.

        // Properties below are in same order as vanilla dedicated server GUI.

        // OTHERS
        [Torch.Views.Display(Description = "Enables adaptive simulation quality system. This system is useful if you have a lot of voxel deformations in the world and low simulation speed.", Name = "Adaptive Simulation Quality", GroupName = "Others")]
        public bool AdaptiveSimulationQuality { get => _settings.AdaptiveSimulationQuality; set => SetValue(ref _settings.AdaptiveSimulationQuality, value); }

        [Torch.Views.Display(Description = "Defines autosave interval.", Name = "Autosave Interval [mins]", GroupName = "Others")]
        public uint AutoSaveInMinutes { get => _settings.AutoSaveInMinutes; set => SetValue(ref _settings.AutoSaveInMinutes, value); }

        [Torch.Views.Display(Description = "Enables drop containers (unknown signals).", Name = "Enable Drop Containers", GroupName = "Others")]
        public bool EnableContainerDrops { get => _settings.EnableContainerDrops; set => SetValue(ref _settings.EnableContainerDrops, value); }

        [Torch.Views.Display(Description = "Enables possibility of converting grid to station.", Name = "Enable Convert to Station", GroupName = "Others")]
        public bool EnableConvertToStation { get => _settings.EnableConvertToStation; set => SetValue(ref _settings.EnableConvertToStation, value); }

        [Torch.Views.Display(Description = "Enables Good.bot hints in the world. If user has disabled hints, this will not override that.", Name = "Enable Good.bot hints", GroupName = "Others")]
        public bool EnableGoodBotHints { get => _settings.EnableGoodBotHints; set => SetValue(ref _settings.EnableGoodBotHints, value); }

        [Torch.Views.Display(Description = "Enable component handling the match.", Name = "Enable Match", GroupName = "Others")]
        public bool EnableMatchComponent { get => _settings.EnableMatchComponent; set => SetValue(ref _settings.EnableMatchComponent, value); }

        [Torch.Views.Display(Description = "Enables possibility to remove grid remotely from the world by an author.", Name = "Enable Remote Grid Removal", GroupName = "Others")]
        public bool EnableRemoteBlockRemoval { get => _settings.EnableRemoteBlockRemoval; set => SetValue(ref _settings.EnableRemoteBlockRemoval, value); }

        [Torch.Views.Display(Description = "Enables research progression.", Name = "Enable Progression", GroupName = "Others")]
        public bool EnableResearch { get => _settings.EnableResearch; set => SetValue(ref _settings.EnableResearch, value); }

        [Torch.Views.Display(Description = "Enables respawn ships.", Name = "Enable Respawn Ships", GroupName = "Others")]
        public bool EnableRespawnShips { get => _settings.EnableRespawnShips; set => SetValue(ref _settings.EnableRespawnShips, value); }

        [Torch.Views.Display(Description = "Enables saving from the menu.", Name = "Enable Saving from Menu", GroupName = "Others")]
        public bool EnableSaving { get => _settings.EnableSaving; set => SetValue(ref _settings.EnableSaving, value); }

        [Torch.Views.Display(Description = "Adds a Scripter role, only Scripters and higher ranks will be able to paste and modify scripts.", Name = "Enable Scripter Role", GroupName = "Others")]
        public bool EnableScripterRole { get => _settings.EnableScripterRole; set => SetValue(ref _settings.EnableScripterRole, value); }

        [Torch.Views.Display(Description = "When enabled game will update physics only in the specific clusters, which are necessary. Dedicated server options only.", Name = "Enable Selective Physics Updates", GroupName = "Others")]
        public bool EnableSelectivePhysics { get => _settings.EnableSelectivePhysicsUpdates; set => SetValue(ref _settings.EnableSelectivePhysicsUpdates, value); }

        [Torch.Views.Display(Description = "Show team scores at the top of the screen.", Name = "Enable Team Score Counters", GroupName = "Others")]
        public bool EnableTeamScoreCounters { get => _settings.EnableTeamScoreCounters; set => SetValue(ref _settings.EnableTeamScoreCounters, value); }

        [Torch.Views.Display(Description = "Enables voxel hand.", Name = "Enable voxel hand", GroupName = "Others")]
        public bool EnableVoxelHand { get => _settings.EnableVoxelHand; set => SetValue(ref _settings.EnableVoxelHand, value); }

        [Torch.Views.Display(Description = "The type of the game mode.", Name = "Game Mode", GroupName = "Others")]
        public MyGameModeEnum GameMode { get => _settings.GameMode; set => SetValue(ref _settings.GameMode, value); }

        [Torch.Views.Display(Description = "Enables infinite ammunition in survival game mode.", Name = "Enable Infinite Ammunition in Survival", GroupName = "Others")]
        public bool InfiniteAmmo { get => _settings.InfiniteAmmo; set => SetValue(ref _settings.InfiniteAmmo, value); }

        [Torch.Views.Display(Description = "Duration of Match phase of the match.", Name = "Match Duration", GroupName = "Others")]
        public float MatchDuration { get => _settings.MatchDuration; set => SetValue(ref _settings.MatchDuration, value); }

        [Torch.Views.Display(Description = "The maximum number of backup saves.", Name = "Max Backup Saves", GroupName = "Others")]
        public short MaxBackupSaves { get => _settings.MaxBackupSaves; set => SetValue(ref _settings.MaxBackupSaves, value); }

        [Torch.Views.Display(Description = "Defines maximum respawn time for drop containers.", Name = "Max Drop Container Respawn Time", GroupName = "Others")]
        public int MaxDropContainerRespawnTime { get => _settings.MaxDropContainerRespawnTime; set => SetValue(ref _settings.MaxDropContainerRespawnTime, value); }

        [Torch.Views.Display(Description = "Maximum number of messages displayed in HUD chat.", Name = "Max messages in HUD chat", GroupName = "Others")]
        public int MaxHudChatMessageCount { get => _settings.MaxHudChatMessageCount; set => SetValue(ref _settings.MaxHudChatMessageCount, value); }

        [Torch.Views.Display(Description = "Limit maximum number of types of planets in the world.", Name = "Max Planet Types", GroupName = "Others")]
        public int MaxPlanets { get => _settings.MaxPlanets; set => SetValue(ref _settings.MaxPlanets, value); }

        [Torch.Views.Display(Description = "Maximum allowed length of a production queue. Can affect performance.", Name = "Max Production Queue Length", GroupName = "Others")]
        public int MaxProductionQueueLength { get => _settings.MaxProductionQueueLength; set => SetValue(ref _settings.MaxProductionQueueLength, value); }

        [Torch.Views.Display(Description = "Defines minimum respawn time for drop containers.", Name = "Min Drop Container Respawn Time", GroupName = "Others")]
        public int MinDropContainerRespawnTime { get => _settings.MinDropContainerRespawnTime; set => SetValue(ref _settings.MinDropContainerRespawnTime, value); }

        [Torch.Views.Display(Description = "Filter offensive words from all input methods.", Name = "Offensive Words Filtering", GroupName = "Others")]
        public bool OffensiveWordsFiltering { get => _settings.OffensiveWordsFiltering; set => SetValue(ref _settings.OffensiveWordsFiltering, value); }

        [Torch.Views.Display(Description = "Duration of PostMatch phase of the match.", Name = "PostMatch Duration", GroupName = "Others")]
        public float PostMatchDuration { get => _settings.PostMatchDuration; set => SetValue(ref _settings.PostMatchDuration, value); }

        [Torch.Views.Display(Description = "To conserve memory, predefined asteroids has to be disabled on consoles.", Name = "Enable Predefined Asteroids", GroupName = "Others")]
        public bool PredefinedAsteroids { get => _settings.PredefinedAsteroids; set => SetValue(ref _settings.PredefinedAsteroids, value); }

        [Torch.Views.Display(Description = "Duration of PreMatch phase of the match.", Name = "PreMatch Duration", GroupName = "Others")]
        public float PreMatchDuration { get => _settings.PreMatchDuration; set => SetValue(ref _settings.PreMatchDuration, value); }

        [Torch.Views.Display(Description = "When enabled respawn ship is removed after player logout.", Name = "Remove Respawn Ships on Logoff", GroupName = "Others")]
        public bool RespawnShipDelete { get => _settings.RespawnShipDelete; set => SetValue(ref _settings.RespawnShipDelete, value); }

        [Torch.Views.Display(Description = "Enables thruster damage.", Name = "Enable Thruster Damage", GroupName = "Others")]
        public bool ThrusterDamage { get => _settings.ThrusterDamage; set => SetValue(ref _settings.ThrusterDamage, value); }

        [Torch.Views.Display(Description = "To conserve memory, some of the blocks have different PCU values for consoles.", Name = "Use Console PCU", GroupName = "Others")]
        public bool UseConsolePCU { get => _settings.UseConsolePCU; set => SetValue(ref _settings.UseConsolePCU, value); }

        [Torch.Views.Display(Description = "Enables weapons.", Name = "Enable Weapons", GroupName = "Others")]
        public bool WeaponsEnabled { get => _settings.WeaponsEnabled; set => SetValue(ref _settings.WeaponsEnabled, value); }

        // This setting is not found in vanilla dedicated server GUI
        [Torch.Views.Display(Description = "The type of the game online mode.", Name = "Online Mode", GroupName = "Others")]
        public MyOnlineModeEnum OnlineMode { get => _settings.OnlineMode; set => SetValue(ref _settings.OnlineMode, value); }



        // TRASH REMOVAL
        [Torch.Views.Display(Description = "Defines time in minutes after which inactive players will be kicked. 0 is off.", Name = "AFK Timeout", GroupName = "Trash Removal")]
        public int AFKTimeountMin { get => _settings.AFKTimeountMin; set => SetValue(ref _settings.AFKTimeountMin, value); }

        [Torch.Views.Display(Description = "Defines block count threshold for trash removal system.", Name = "Block Count Threshold", GroupName = "Trash Removal")]
        public int BlockCountThreshold { get => _settings.BlockCountThreshold; set => SetValue(ref _settings.BlockCountThreshold, value); }

        [Torch.Views.Display(Description = "Enable trash settings to be overriden by console specific settings.", Name = "Platform Trash Setting Override", GroupName = "Trash Removal")]
        public bool EnableTrashSettingsPlatformOverride { get => _settings.EnableTrashSettingsPlatformOverride; set => SetValue(ref _settings.EnableTrashSettingsPlatformOverride, value); }

        [Torch.Views.Display(Description = "By setting this, server will keep number of grids around this value. \n !WARNING! It ignores Powered and Fixed flags, Block Count and lowers Distance from player.\n Set to 0 to disable.", Name = "Optimal Grid Count", GroupName = "Trash Removal")]
        public int OptimalGridCount { get => _settings.OptimalGridCount; set => SetValue(ref _settings.OptimalGridCount, value); }

        [Torch.Views.Display(Description = "Defines character removal threshold for trash removal system. If player disconnects it will remove his character after this time.\n Set to 0 to disable.", Name = "Character Removal Threshold [mins]", GroupName = "Trash Removal")]
        public int PlayerCharacterRemovalThreshold { get => _settings.PlayerCharacterRemovalThreshold; set => SetValue(ref _settings.PlayerCharacterRemovalThreshold, value); }

        [Torch.Views.Display(Description = "Defines player distance threshold for trash removal system.", Name = "Player Distance Threshold [m]", GroupName = "Trash Removal")]
        public float PlayerDistanceThreshold { get => _settings.PlayerDistanceThreshold; set => SetValue(ref _settings.PlayerDistanceThreshold, value); }

        [Torch.Views.Display(Description = "Defines player inactivity threshold for trash removal system. \n !WARNING! This will remove all grids of the player.\n Set to 0 to disable.", Name = "Player Inactivity Threshold [hours]", GroupName = "Trash Removal")]
        public float PlayerInactivityThreshold { get => _settings.PlayerInactivityThreshold; set => SetValue(ref _settings.PlayerInactivityThreshold, value); }

        [Torch.Views.Display(Description = "Defines time in hours after which inactive identities that do not own any grids will be removed. Set 0 to disable.", Name = "Remove Old Identities (h)", GroupName = "Trash Removal")]
        public int RemoveOldIdentitiesH { get => _settings.RemoveOldIdentitiesH; set => SetValue(ref _settings.RemoveOldIdentitiesH, value); }

        [Torch.Views.Display(Description = "Defines time in minutes after which grids will be stopped if far from player. Set 0 to disable.", Name = "Stop Grids Period (m)", GroupName = "Trash Removal")]
        public int StopGridsPeriodMin { get => _settings.StopGridsPeriodMin; set => SetValue(ref _settings.StopGridsPeriodMin, value); }

        [Torch.Views.Display(Description = "Defines flags for trash removal system.", Name = "Trash Removal Flags", GroupName = "Trash Removal")]
        public MyTrashRemovalFlags TrashFlagsValue { get => (MyTrashRemovalFlags)_settings.TrashFlagsValue; set => SetValue(ref _settings.TrashFlagsValue, (int)value); }

        [Torch.Views.Display(Description = "Enables trash removal system.", Name = "Trash Removal Enabled", GroupName = "Trash Removal")]
        public bool TrashRemovalEnabled { get => _settings.TrashRemovalEnabled; set => SetValue(ref _settings.TrashRemovalEnabled, value); }

        [Torch.Views.Display(Description = "Only voxel chunks that have been modified longer time age may be reverted.", Name = "Voxel Age (min)", GroupName = "Trash Removal")]
        public int VoxelAgeThreshold { get => _settings.VoxelAgeThreshold; set => SetValue(ref _settings.VoxelAgeThreshold, value); }

        [Torch.Views.Display(Description = "Only voxel chunks that are further from any grid will be reverted.", Name = "Distance Voxel from Grid (m)", GroupName = "Trash Removal")]
        public float VoxelGridDistanceThreshold { get => _settings.VoxelGridDistanceThreshold; set => SetValue(ref _settings.VoxelGridDistanceThreshold, value); }

        [Torch.Views.Display(Description = "Only voxel chunks that are further from player will be reverted.", Name = "Distance Voxel from Player (m)", GroupName = "Trash Removal")]
        public float VoxelPlayerDistanceThreshold { get => _settings.VoxelPlayerDistanceThreshold; set => SetValue(ref _settings.VoxelPlayerDistanceThreshold, value); }

        [Torch.Views.Display(Description = "Enables system for voxel reverting.", Name = "Enable Voxel Reverting", GroupName = "Trash Removal")]
        public bool VoxelTrashRemovalEnabled { get => _settings.VoxelTrashRemovalEnabled; set => SetValue(ref _settings.VoxelTrashRemovalEnabled, value); }



        // MULTIPLIERS
        [Torch.Views.Display(Description = "The multiplier for assembler efficiency.", Name = "Assembler Efficiency", GroupName = "Multipliers")]
        public float AssemblerEfficiencyMultiplier { get => _settings.AssemblerEfficiencyMultiplier; set => SetValue(ref _settings.AssemblerEfficiencyMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for assembler speed.", Name = "Assembler Speed", GroupName = "Multipliers")]
        public float AssemblerSpeedMultiplier { get => _settings.AssemblerSpeedMultiplier; set => SetValue(ref _settings.AssemblerSpeedMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for inventory size for the blocks.", Name = "Blocks Inventory Size", GroupName = "Multipliers")]
        public float BlockInventorySizeMultiplier { get => _settings.BlocksInventorySizeMultiplier; set => SetValue(ref _settings.BlocksInventorySizeMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for grinder speed.", Name = "Grinder Speed", GroupName = "Multipliers")]
        public float GrinderSpeedMultiplier { get => _settings.GrinderSpeedMultiplier; set => SetValue(ref _settings.GrinderSpeedMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for hacking speed.", Name = "Hacking Speed", GroupName = "Multipliers")]
        public float HackSpeedMultiplier { get => _settings.HackSpeedMultiplier; set => SetValue(ref _settings.HackSpeedMultiplier, value); }

        [Torch.Views.Display(Description = "Harvest ratio multiplier for drills.", Name = "Harvest Ratio Multiplier", GroupName = "Multipliers")]
        public float HarvestRatioMultiplier { get => _settings.HarvestRatioMultiplier; set => SetValue(ref _settings.HarvestRatioMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for inventory size for the characters.", Name = "Characters Inventory Size", GroupName = "Multipliers")]
        public float CharacterInventorySizeMultiplier { get => _settings.InventorySizeMultiplier; set => SetValue(ref _settings.InventorySizeMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for refinery speed.", Name = "Refinery Speed", GroupName = "Multipliers")]
        public float RefinerySpeedMultiplier { get => _settings.RefinerySpeedMultiplier; set => SetValue(ref _settings.RefinerySpeedMultiplier, value); }

        [Torch.Views.Display(Description = "The multiplier for welder speed.", Name = "Welder Speed", GroupName = "Multipliers")]
        public float WelderSpeedMultiplier { get => _settings.WelderSpeedMultiplier; set => SetValue(ref _settings.WelderSpeedMultiplier, value); }



        // PLAYERS
        [Torch.Views.Display(Description = "Auto-healing heals players only in oxygen environments and during periods of not taking damage.", Name = "Auto Healing", GroupName = "Players")]
        public bool AutoHealing { get => _settings.AutoHealing; set => SetValue(ref _settings.AutoHealing, value); }

        [Torch.Views.Display(Description = "Sets the timer (minutes) for the backpack to be removed from the world. Default is 5 minutes.", Name = "Backpack Despawn Time", GroupName = "Players")]
        public float BackpackDespawnTimer { get => _settings.BackpackDespawnTimer; set => SetValue(ref _settings.BackpackDespawnTimer, value); }

        [Torch.Views.Display(Description = "Enable sharing of blueprints", Name = "Enable Blueprint Share", GroupName = "Players")]
        public bool BlueprintShare { get => _settings.BlueprintShare; set => SetValue(ref _settings.BlueprintShare, value); }

        [Torch.Views.Display(Description = "Timeout between sharing blueprints.", Name = "Share Blueprint Timeout", GroupName = "Players")]
        public int BlueprintShareTimeout { get => _settings.BlueprintShareTimeout; set => SetValue(ref _settings.BlueprintShareTimeout, value); }

        [Torch.Views.Display(Description = "Speed multiplier for player characters.", Name = "Character Speed Multiplier", GroupName = "Players")]
        public float CharacterSpeedMultiplier { get => _settings.CharacterSpeedMultiplier; set => SetValue(ref _settings.CharacterSpeedMultiplier, value); }

        [Torch.Views.Display(Description = "Enables 3rd person camera.", Name = "Enable 3rd Person Camera", GroupName = "Players")]
        public bool Enable3rdPersonView { get => _settings.Enable3rdPersonView; set => SetValue(ref _settings.Enable3rdPersonView, value); }

        [Torch.Views.Display(Description = "Enables automatic respawn at nearest available respawn point.", Name = "Enable Auto Respawn", GroupName = "Players")]
        public bool EnableAutoRespawn { get => _settings.EnableAutorespawn; set => SetValue(ref _settings.EnableAutorespawn, value); }

        [Torch.Views.Display(Description = "If enabled bounty contracts will be available on stations.", Name = "Enable Bounty Contracts", GroupName = "Players")]
        public bool EnableBountyContracts { get => _settings.EnableBountyContracts; set => SetValue(ref _settings.EnableBountyContracts, value); }

        [Torch.Views.Display(Description = "Enables copy and paste feature.", Name = "Enable Copy & Paste", GroupName = "Players")]
        public bool EnableCopyPaste { get => _settings.EnableCopyPaste; set => SetValue(ref _settings.EnableCopyPaste, value); }

        [Torch.Views.Display(Description = "Enable aim assist for gamepad.", Name = "Enable Gamepad Aim Assist", GroupName = "Players")]
        public bool EnableGamepadAimAssist { get => _settings.EnableGamepadAimAssist; set => SetValue(ref _settings.EnableGamepadAimAssist, value); }

        [Torch.Views.Display(Description = "Enables jetpack.", Name = "Enable Jetpack", GroupName = "Players")]
        public bool EnableJetpack { get => _settings.EnableJetpack; set => SetValue(ref _settings.EnableJetpack, value); }

        [Torch.Views.Display(Description = "Enable trading of PCUs between players or factions depending on PCU settings.", Name = "Enable PCU Trading", GroupName = "Players")]
        public bool EnablePCUTrading { get => _settings.EnablePcuTrading; set => SetValue(ref _settings.EnablePcuTrading, value); }

        [Torch.Views.Display(Description = "Enable weapon recoil on player weapons.", Name = "Enable Weapon Recoil", GroupName = "Players")]
        public bool EnableRecoil { get => _settings.EnableRecoil; set => SetValue(ref _settings.EnableRecoil, value); }

        [Torch.Views.Display(Description = "Enables player to respawn in space suit.", Name = "Enable Space Suit Respawn", GroupName = "Players")]
        public bool EnableSpaceSuitRespawn { get => _settings.EnableSpaceSuitRespawn; set => SetValue(ref _settings.EnableSpaceSuitRespawn, value); }

        [Torch.Views.Display(Description = "Enables tool shake feature.", Name = "Enable Tool Shake", GroupName = "Players")]
        [DefaultValue(false)]
        public bool EnableToolShake { get => _settings.EnableToolShake; set => SetValue(ref _settings.EnableToolShake, value); }

        [Torch.Views.Display(Description = "This multiplier only applies for damage caused to the player by environment.", Name = "Environment Damage Multiplier", GroupName = "Players")]
        public float EnvironmentDamageMultiplier { get => _settings.EnvironmentDamageMultiplier; set => SetValue(ref _settings.EnvironmentDamageMultiplier, value); }

        [Torch.Views.Display(Description = "Enables shared accounts to join multiplayer games.", Name = "Enable Family Sharing", GroupName = "Players")]
        public bool EnableFamilySharing { get => _settings.FamilySharing; set => SetValue(ref _settings.FamilySharing, value); }

        [Torch.Views.Display(Description = "The maximum number of connected players.", Name = "Max Players", GroupName = "Players")]
        public short MaxPlayers { get => _settings.MaxPlayers; set => SetValue(ref _settings.MaxPlayers, value); }

        [Torch.Views.Display(Description = "Sets optimal distance in meters the game should take into consideration when spawning new player near others.", Name = "Optimal Respawn Distance", GroupName = "Players")]
        public float OptimalSpawnDistance { get => _settings.OptimalSpawnDistance; set => SetValue(ref _settings.OptimalSpawnDistance, value); }

        [Torch.Views.Display(Description = "Enables if player names should be shown on the HUD.", Name = "Show Player Names on HUD", GroupName = "Players")]
        public bool ShowPlayerNamesOnHud { get => _settings.ShowPlayerNamesOnHud; set => SetValue(ref _settings.ShowPlayerNamesOnHud, value); }

        [Torch.Views.Display(Description = "The multiplier for respawn ship timer.", Name = "Respawn Ship Time Multiplier", GroupName = "Players")]
        public float SpawnShipTimeMultiplier { get => _settings.SpawnShipTimeMultiplier; set => SetValue(ref _settings.SpawnShipTimeMultiplier, value); }

        [Torch.Views.Display(Description = "Enables spawning with tools in the inventory.", Name = "Spawn with Tools", GroupName = "Players")]
        public bool SpawnWithTools { get => _settings.SpawnWithTools; set => SetValue(ref _settings.SpawnWithTools, value); }

        // This setting is not found in vanilla dedicated server GUI
        [Torch.Views.Display(Description = "Enables respawn screen.", Name = "Enable Respawn Screen in the Game", GroupName = "Players")]
        public bool StartInRespawnScreen { get => _settings.StartInRespawnScreen; set => SetValue(ref _settings.StartInRespawnScreen, value); }



        // BLOCK LIMITS
        [Torch.Views.Display(Description = "Defines block limits mode.", Name = "Block Limits Mode", GroupName = "Block Limits")]
        public MyBlockLimitsEnabledEnum BlockLimitsEnabled { get => _settings.BlockLimitsEnabled; set => SetValue(ref _settings.BlockLimitsEnabled, value); }

        [Torch.Views.Display(Description = "The maximum number of blocks per player.", Name = "Max Blocks per Player", GroupName = "Block Limits")]
        public int MaxBlocksPerPlayer { get => _settings.MaxBlocksPerPlayer; set => SetValue(ref _settings.MaxBlocksPerPlayer, value); }

        [Torch.Views.Display(Description = "The maximum number of existing factions in the world.", Name = "Max Factions Count", GroupName = "Block Limits")]
        public int MaxFactionsCount { get => _settings.MaxFactionsCount; set => SetValue(ref _settings.MaxFactionsCount, value); }

        [Torch.Views.Display(Description = "The maximum number of blocks in one grid.", Name = "Max Grid Blocks", GroupName = "Block Limits")]
        public int MaxGridSize { get => _settings.MaxGridSize; set => SetValue(ref _settings.MaxGridSize, value); }

        [Torch.Views.Display(Description = "The total number of Performance Cost Units in the world.", Name = "World PCU", GroupName = "Block Limits")]
        public int TotalPCU { get => _settings.TotalPCU; set => SetValue(ref _settings.TotalPCU, value); }

        [Torch.Views.Display(Name = "Block Type World Limits", GroupName = "Block Limits")]
        public Dictionary<string, short> BlockTypeLimits { get => _settings.BlockTypeLimits.Dictionary; set => SetValue(x => _settings.BlockTypeLimits.Dictionary = x, value); }



        // ENVIRONMENT
        [Torch.Views.Display(Description = "The maximum distance Broadcast Controller will transmit messages when its owner is offline.", Name = "Offline Broadcast Controller Distance [m]", GroupName = "Environment")]
        public int BroadcastControllerMaxOfflineTransmitDistance { get => _settings.BroadcastControllerMaxOfflineTransmitDistance; set => SetValue(ref _settings.BroadcastControllerMaxOfflineTransmitDistance, value); }

        [Torch.Views.Display(Description = "Enables destruction feature for the blocks.", Name = "Enable Destructible Blocks", GroupName = "Environment")]
        public bool DestructibleBlocks { get => _settings.DestructibleBlocks; set => SetValue(ref _settings.DestructibleBlocks, value); }

        [Torch.Views.Display(Description = "Enables oxygen in the world.", Name = "Enable Oxygen", GroupName = "Environment")]
        public bool EnableOxygen { get => _settings.EnableOxygen; set => SetValue(ref _settings.EnableOxygen, value); }

        [Torch.Views.Display(Description = "Enables airtightness in the world.", Name = "Enable Airtightness", GroupName = "Environment")]
        public bool EnableOxygenPressurization { get => _settings.EnableOxygenPressurization; set => SetValue(ref _settings.EnableOxygenPressurization, value); }

        [Torch.Views.Display(Description = "Enables sun rotation.", Name = "Enable Sun Rotation", GroupName = "Environment")]
        public bool EnableSunRotation { get => _settings.EnableSunRotation; set => SetValue(ref _settings.EnableSunRotation, value); }

        [Torch.Views.Display(Description = "Enable explosion damage from missiles/turrets being applied to its own grid.", Name = "Enable Friendly Missile/Turret Damage", GroupName = "Environment")]
        public bool EnableTurretsFriendlyFire { get => _settings.EnableTurretsFriendlyFire; set => SetValue(ref _settings.EnableTurretsFriendlyFire, value); }

        [Torch.Views.Display(Description = "Enables voxel destructions.", Name = "Enable Voxel Destruction", GroupName = "Environment")]
        public bool EnableVoxelDestruction { get => _settings.EnableVoxelDestruction; set => SetValue(ref _settings.EnableVoxelDestruction, value); }

        [Torch.Views.Display(Description = "Defines density of the procedurally generated encounters.", Name = "Encounter Density", GroupName = "Environment")]
        public float EncounterDensity { get => _settings.EncounterDensity; set => SetValue(ref _settings.EncounterDensity, value); }

        [Torch.Views.Display(Description = "Defines hostility of the environment.", Name = "Environment Hostility", GroupName = "Environment")]
        public MyEnvironmentHostilityEnum EnvironmentHostility { get => _settings.EnvironmentHostility; set => SetValue(ref _settings.EnvironmentHostility, value); }

        [Torch.Views.Display(Description = "The maximum number of existing cargo bags.", Name = "Max Cargo Bags", GroupName = "Environment")]
        public short MaxCargoBags { get => _settings.MaxCargoBags; set => SetValue(ref _settings.MaxCargoBags, value); }

        [Torch.Views.Display(Description = "The maximum number of existing floating objects.", Name = "Max Floating Objects", GroupName = "Environment")]
        public short MaxFloatingObjects { get => _settings.MaxFloatingObjects; set => SetValue(ref _settings.MaxFloatingObjects, value); }

        [Torch.Views.Display(Description = "World size can't be selected lower than this value.", Name = "Minimum World Size [km]", GroupName = "Environment")]
        public int MinimumWorldSize { get => _settings.MinimumWorldSize; set => SetValue(ref _settings.MinimumWorldSize, value); }

        [Torch.Views.Display(Description = "Defines the number of physics iterations.", Name = "Physics Iterations", GroupName = "Environment")]
        public int PhysicsIterations { get => _settings.PhysicsIterations; set => SetValue(ref _settings.PhysicsIterations, value); }

        [Torch.Views.Display(Description = "Defines at what maximum distance weapons could interact with voxels.", Name = "Prefetch Voxels Range Limit", GroupName = "Environment")]
        public long PrefetchShapeRayLengthLimit { get => _settings.PrefetchShapeRayLengthLimit; set => SetValue(ref _settings.PrefetchShapeRayLengthLimit, value); }

        [Torch.Views.Display(Description = "Enables realistic sounds.", Name = "Enable Realistic Sound", GroupName = "Environment")]
        public bool RealisticSound { get => _settings.RealisticSound; set => SetValue(ref _settings.RealisticSound, value); }

        [Torch.Views.Display(Description = "Allow scrap to be dropped from destroyed blocks.", Name = "Enable Scrap Drops", GroupName = "Environment")]
        public bool ScrapEnabled { get => _settings.ScrapEnabled; set => SetValue(ref _settings.ScrapEnabled, value); }

        [Torch.Views.Display(Description = "Defines interval of one rotation of the sun.", Name = "Sun Rotation Interval", GroupName = "Environment")]
        public float SunRotationIntervalMinutes { get => _settings.SunRotationIntervalMinutes; set => SetValue(ref _settings.SunRotationIntervalMinutes, value); }

        [Torch.Views.Display(Description = "Defines synchronization distance in multiplayer. High distance can slow down server drastically. Use with caution.", Name = "Sync Distance", GroupName = "Environment")]
        public int SyncDistance { get => _settings.SyncDistance; set => SetValue(ref _settings.SyncDistance, value); }

        [Torch.Views.Display(Description = "Enable Temporary Containers to spawn after destroying block with inventory.", Name = "Enable Temporary Containers", GroupName = "Environment")]
        public bool TemporaryContainers { get => _settings.TemporaryContainers; set => SetValue(ref _settings.TemporaryContainers, value); }

        [Torch.Views.Display(Description = "Maximum number of organic bots in the world.", Name = "Bot Limit", GroupName = "Environment")]
        public int TotalBotLimit { get => _settings.TotalBotLimit; set => SetValue(ref _settings.TotalBotLimit, value); }

        [Torch.Views.Display(Description = "The maximum amount of time (in minutes) allowed for cargo bags to be alive before deletion.", Name = "Max Cargo Bags Lifetime", GroupName = "Environment")]
        public short TrashCleanerCargoBagsMaxLiveTime { get => _settings.TrashCleanerCargoBagsMaxLiveTime; set => SetValue(ref _settings.TrashCleanerCargoBagsMaxLiveTime, value); }

        [Torch.Views.Display(Description = "Defines the version of the voxel generator.", Name = "Voxel Generator Version", GroupName = "Environment")]
        public int VoxelGeneratorVersion { get => _settings.VoxelGeneratorVersion; set => SetValue(ref _settings.VoxelGeneratorVersion, value); }

        [Torch.Views.Display(Description = "Lightning strikes from weather can damage grids.", Name = "Enable Lightning Damage", GroupName = "Environment")]
        public bool WeatherLightingDamage { get => _settings.WeatherLightingDamage; set => SetValue(ref _settings.WeatherLightingDamage, value); }

        [Torch.Views.Display(Description = "Enable automatic weather generation on planets.", Name = "Enable Weather System", GroupName = "Environment")]
        public bool EnableWeatherSystem { get => _settings.WeatherSystem; set => SetValue(ref _settings.WeatherSystem, value); }

        /// <summary>
        /// Size of the edge of the world area cube.
        /// Don't use directly, as it is error-prone (it's km instead of m and edge size instead of half-extent)
        /// Rather use MyEntities.WorldHalfExtent()
        /// </summary>
        [Torch.Views.Display(Description = "Defines the size of the world.", Name = "World Size [km]", GroupName = "Environment")]
        public int WorldSizeKm { get => _settings.WorldSizeKm; set => SetValue(ref _settings.WorldSizeKm, value); }

        // Below settings are not found in vanilla dedicated server GUI
        [Torch.Views.Display(Description = "Defines density of the procedurally generated asteroids.", Name = "Procedural Asteroids Density", GroupName = "Environment")]
        public float ProceduralDensity { get => _settings.ProceduralDensity; set => SetValue(ref _settings.ProceduralDensity, value); }

        [Torch.Views.Display(Description = "Defines unique starting seed for the procedurally generated content.", Name = "Procedural Seed", GroupName = "Environment")]
        public int ProceduralSeed { get => _settings.ProceduralSeed; set => SetValue(ref _settings.ProceduralSeed, value); }

        [Torch.Views.Display(Description = "Define the density multiplier for flora.", Name = "Flora Density Multiplier", GroupName = "Environment")]
        public float FloraDensityMultiplier { get => _settings.FloraDensityMultiplier; set => SetValue(ref _settings.FloraDensityMultiplier, value); }

        [Torch.Views.Display(Description = "Defines render distance for clients in multiplayer. High distance can slow down client FPS. Values larger than SyncDistance may not work as expected.", Name = "View Distance", GroupName = "Environment")]
        public int ViewDistance { get => _settings.ViewDistance; set => SetValue(ref _settings.ViewDistance, value); }

        [Torch.Views.Display(Description = "Resource deposits count coefficient for generated world content (voxel generator version > 2).", Name = "Deposits Count Coefficient", GroupName = "Environment")]
        public float DepositsCountCoefficient { get => _settings.DepositsCountCoefficient; set => SetValue(ref _settings.DepositsCountCoefficient, value); }

        [Torch.Views.Display(Description = "Resource deposit size denominator for generated world content (voxel generator version > 2).", Name = "Deposit Size Denominator", GroupName = "Environment")]
        public float DepositSideDenominator { get => _settings.DepositSizeDenominator; set => SetValue(ref _settings.DepositSizeDenominator, value); }



        // NPCS
        [Torch.Views.Display(Description = "Enables spawning of cargo ships.", Name = "Enable Cargo Ships", GroupName = "NPCs")]
        public bool CargoShipsEnabled { get => _settings.CargoShipsEnabled; set => SetValue(ref _settings.CargoShipsEnabled, value); }

        [Torch.Views.Display(Description = "Time period between two economy updates.", Name = "Economy tick time", GroupName = "NPCs")]
        public int EconomyTickInSeconds { get => _settings.EconomyTickInSeconds; set => SetValue(ref _settings.EconomyTickInSeconds, value); }

        [Torch.Views.Display(Description = "Enables spawning of drones in the world.", Name = "Enable Drones", GroupName = "NPCs")]
        public bool EnableDrones { get => _settings.EnableDrones; set => SetValue(ref _settings.EnableDrones, value); }

        [Torch.Views.Display(Description = "Enables economy features.", Name = "Enable Economy", GroupName = "NPCs")]
        public bool EnableEconomy { get => _settings.EnableEconomy; set => SetValue(ref _settings.EnableEconomy, value); }

        [Torch.Views.Display(Description = "Enables random encounters in the world.", Name = "Enable Encounters", GroupName = "NPCs")]
        public bool EnableEncounters { get => _settings.EnableEncounters; set => SetValue(ref _settings.EnableEncounters, value); }

        [Torch.Views.Display(Description = "Enable advanced Optimal Reciprocal Collision Avoidance.", Name = "Enable ORCA", GroupName = "NPCs")]
        public bool EnableOrca { get => _settings.EnableOrca; set => SetValue(ref _settings.EnableOrca, value); }

        [Torch.Views.Display(Description = "Enables planetary encounters in the world.", Name = "Enable Planetary Encounters", GroupName = "NPCs")]
        public bool EnablePlanetaryEncounters { get => _settings.EnablePlanetaryEncounters; set => SetValue(ref _settings.EnablePlanetaryEncounters, value); }

        [Torch.Views.Display(Description = "Enables spawning of spiders in the world.", Name = "Enable Spiders", GroupName = "NPCs")]
        public bool EnableSpiders { get => _settings.EnableSpiders; set => SetValue(ref _settings.EnableSpiders, value); }

        [Torch.Views.Display(Description = "Enables spawning of wolves in the world.", Name = "Enable Wolves", GroupName = "NPCs")]
        public bool EnableWolfs { get => _settings.EnableWolfs; set => SetValue(ref _settings.EnableWolfs, value); }

        [Torch.Views.Display(Description = "Maximum of active Global Encounters at the same time. Turned off when 0.", Name = "Global Encounter Cap", GroupName = "NPCs")]
        public int GlobalEncounterCap { get => _settings.GlobalEncounterCap; set => SetValue(ref _settings.GlobalEncounterCap, value); }

        [Torch.Views.Display(Description = "Enable of removal of Global Encounters.", Name = "Global Encounter Enable Removal", GroupName = "NPCs")]
        public bool GlobalEncounterEnableRemovalTimer { get => _settings.GlobalEncounterEnableRemovalTimer; set => SetValue(ref _settings.GlobalEncounterEnableRemovalTimer, value); }

        [Torch.Views.Display(Description = "Maximum removal timer [minutes] for Global Encounters.", Name = "Removal Timer Max [mins]", GroupName = "NPCs")]
        public int GlobalEncounterMaxRemovalTimer { get => _settings.GlobalEncounterMaxRemovalTimer; set => SetValue(ref _settings.GlobalEncounterMaxRemovalTimer, value); }

        [Torch.Views.Display(Description = "Minimum removal timer [minutes] for Global Encounters.", Name = "Removal Timer Min [mins]", GroupName = "NPCs")]
        public int GlobalEncounterMinRemovalTimer { get => _settings.GlobalEncounterMinRemovalTimer; set => SetValue(ref _settings.GlobalEncounterMinRemovalTimer, value); }

        [Torch.Views.Display(Description = "Number of Performance Cost Units allocated for global encounters.", Name = "Global Encounter PCU", GroupName = "NPCs")]
        public int GlobalEncounterPCU { get => _settings.GlobalEncounterPCU; set => SetValue(ref _settings.GlobalEncounterPCU, value); }

        [Torch.Views.Display(Description = "When the game should start countdown for removal of Global Encounter.", Name = "Removal Time Clock [mins]", GroupName = "NPCs")]
        public int GlobalEncounterRemovalTimeClock { get => _settings.GlobalEncounterRemovalTimeClock; set => SetValue(ref _settings.GlobalEncounterRemovalTimeClock, value); }

        [Torch.Views.Display(Description = "Global Encounter spawns every X minutes.", Name = "Global Encounter Timer [min]", GroupName = "NPCs")]
        public int GlobalEncounterTimer { get => _settings.GlobalEncounterTimer; set => SetValue(ref _settings.GlobalEncounterTimer, value); }

        [Torch.Views.Display(Description = "Time period in which player can claim NPC grid. NPC block do despawn after limit ends.", Name = "Claim Time for NPC Grids", GroupName = "NPCs")]
        public int NPCGridClaimTimeLimit { get => _settings.NPCGridClaimTimeLimit; set => SetValue(ref _settings.NPCGridClaimTimeLimit, value); }

        [Torch.Views.Display(Description = "Number of Performance Cost Units allocated for NPC factions.", Name = "NPC PCU", GroupName = "NPCs")]
        public int PiratePCU { get => _settings.PiratePCU; set => SetValue(ref _settings.PiratePCU, value); }

        [Torch.Views.Display(Description = "Claim area radius for Planetary Encounter spawns - no more installations would spawn there until clean-up.", Name = "Existing installations allowed [m]", GroupName = "NPCs")]
        public int PlanetaryEncounterAreaLockdownRange { get => _settings.PlanetaryEncounterAreaLockdownRange; set => SetValue(ref _settings.PlanetaryEncounterAreaLockdownRange, value); }

        [Torch.Views.Display(Description = "Player-based distance for Planetary Encounter spawns.", Name = "Desired spawn distance [m]", GroupName = "NPCs")]
        public int PlanetaryEncounterDesiredSpawnRange { get => _settings.PlanetaryEncounterDesiredSpawnRange; set => SetValue(ref _settings.PlanetaryEncounterDesiredSpawnRange, value); }

        [Torch.Views.Display(Description = "Timeout [minutes] for permanent installation auto-removal.", Name = "Installations despawn timeout [mins]", GroupName = "NPCs")]
        public float PlanetaryEncounterDespawnTimeout { get => _settings.PlanetaryEncounterDespawnTimeout; set => SetValue(ref _settings.PlanetaryEncounterDespawnTimeout, value); }

        [Torch.Views.Display(Description = "Minimum distance to existing structures for Planetary Encounter spawns.", Name = "Existing structures allowed [m]", GroupName = "NPCs")]
        public int PlanetaryEncounterExistingStructuresRange { get => _settings.PlanetaryEncounterExistingStructuresRange; set => SetValue(ref _settings.PlanetaryEncounterExistingStructuresRange, value); }

        [Torch.Views.Display(Description = "Player presence within this range prevents installation auto-removal.", Name = "Presence distance [m]", GroupName = "NPCs")]
        public int PlanetaryEncounterPresenceRange { get => _settings.PlanetaryEncounterPresenceRange; set => SetValue(ref _settings.PlanetaryEncounterPresenceRange, value); }

        [Torch.Views.Display(Description = "First interval [minutes] for Planetary Encounter spawn timer.", Name = "Installations Timer First [mins]", GroupName = "NPCs")]
        public float PlanetaryEncounterTimerFirst { get => _settings.PlanetaryEncounterTimerFirst; set => SetValue(ref _settings.PlanetaryEncounterTimerFirst, value); }

        [Torch.Views.Display(Description = "Maximum [minutes] for Planetary Encounter spawn timer.", Name = "Installations Timer Max [mins]", GroupName = "NPCs")]
        public float PlanetaryEncounterTimerMax { get => _settings.PlanetaryEncounterTimerMax; set => SetValue(ref _settings.PlanetaryEncounterTimerMax, value); }

        [Torch.Views.Display(Description = "Minimum [minutes] for Planetary Encounter spawn timer.", Name = "Installations Timer Min [mins]", GroupName = "NPCs")]
        public float PlanetaryEncounterTimerMin { get => _settings.PlanetaryEncounterTimerMin; set => SetValue(ref _settings.PlanetaryEncounterTimerMin, value); }

        [Torch.Views.Display(Description = "The inner radius [m] (center is in 0,0,0), where stations can spawn. Does not affect planet-bound stations (surface Outposts and Orbital stations).", Name = "Stations Inner Radius", GroupName = "NPCs")]
        public double StationsDistanceInnerRadius { get => _settings.StationsDistanceInnerRadius; set => SetValue(ref _settings.StationsDistanceInnerRadius, value); }

        [Torch.Views.Display(Description = "The outer radius [m] (center is in 0,0,0), where stations can spawn. Does not affect planet-bound stations (surface Outposts and Orbital stations).", Name = "Stations Outer Radius End", GroupName = "NPCs")]
        public double StationsDistanceOuterRadiusEnd { get => _settings.StationsDistanceOuterRadiusEnd; set => SetValue(ref _settings.StationsDistanceOuterRadiusEnd, value); }

        [Torch.Views.Display(Description = "The outer radius [m] (center is in 0,0,0), where stations can spawn. Does not affect planet-bound stations (surface Outposts and Orbital stations).", Name = "Stations Outer Radius Start", GroupName = "NPCs")]
        public double StationsDistanceOuterRadiusStart { get => _settings.StationsDistanceOuterRadiusStart; set => SetValue(ref _settings.StationsDistanceOuterRadiusStart, value); }

        [Torch.Views.Display(Description = "The number of NPC factions generated on the start of the world.", Name = "NPC Factions Count", GroupName = "NPCs")]
        public int TradeFactionsCount { get => _settings.TradeFactionsCount; set => SetValue(ref _settings.TradeFactionsCount, value); }

        // This setting is not found in vanilla dedicated server GUI
        [Torch.Views.Display(Description = "Version of the encounter generator system.", Name = "Encounter Generator Version", GroupName = "NPCs")]
        public int EncounterGeneratorVersion { get => _settings.EncounterGeneratorVersion; set => SetValue(ref _settings.EncounterGeneratorVersion, value); }



        // PVP
        [Torch.Views.Display(Description = "Faction Voice Chat removes the need of antennas and broadcasting of the character for faction.", Name = "Enable Faction Voice Chat", GroupName = "PvP")]
        public bool EnableFactionVoiceChat { get => _settings.EnableFactionVoiceChat; set => SetValue(ref _settings.EnableFactionVoiceChat, value); }

        [Torch.Views.Display(Description = "", Name = "Enable Friendly Fire", GroupName = "PvP")]
        public bool EnableFriendlyFire { get => _settings.EnableFriendlyFire; set => SetValue(ref _settings.EnableFriendlyFire, value); }

        [Torch.Views.Display(Description = "", Name = "Enable Team Balancing", GroupName = "PvP")]
        public bool EnableTeamBalancing { get => _settings.EnableTeamBalancing; set => SetValue(ref _settings.EnableTeamBalancing, value); }

        [Torch.Views.Display(Description = "Defines the maximum distance to show the enemy target indicator.", Name = "Enemy Target Indicator Distance [m]", GroupName = "PvP")]
        public float EnemyTargetIndicatorDistance { get => _settings.EnemyTargetIndicatorDistance; set => SetValue(ref _settings.EnemyTargetIndicatorDistance, value); }

        [Torch.Views.Display(Description = "Server will restart after specified time [minutes], when it's empty after match started. Works only in PvP scenarios. When 0 feature is disabled.", Name = "Match Restart When Empty", GroupName = "PvP")]
        public int MatchRestartWhenEmptyTime { get => _settings.MatchRestartWhenEmptyTime; set => SetValue(ref _settings.MatchRestartWhenEmptyTime, value); }



        // EXPERIMENTAL
        [Torch.Views.Display(Description = "Enables in game scripts.", Name = "Enable Ingame Scripts", GroupName = "Experimental")]
        public bool EnableIngameScripts { get => _settings.EnableIngameScripts; set => SetValue(ref _settings.EnableIngameScripts, value); }

        [Torch.Views.Display(Description = "Enables experimental feature to share inertia tensor.", Name = "Enable Share Inertia Tensor", GroupName = "Experimental")]
        public bool EnableShareInertiaTensor { get => _settings.EnableShareInertiaTensor; set => SetValue(ref _settings.EnableShareInertiaTensor, value); }

        [Torch.Views.Display(Description = "Enables spectator camera.", Name = "Enable Spectator Camera", GroupName = "Experimental")]
        public bool EnableSpectator { get => _settings.EnableSpectator; set => SetValue(ref _settings.EnableSpectator, value); }

        [Torch.Views.Display(Description = "Enables sub-grid damage.", Name = "Enable Sub-Grid Damage", GroupName = "Experimental")]
        public bool EnableSubgridDamage { get => _settings.EnableSubgridDamage; set => SetValue(ref _settings.EnableSubgridDamage, value); }

        [Torch.Views.Display(Description = "Allows super gridding exploit to be used.", Name = "Enable Supergridding", GroupName = "Experimental")]
        public bool EnableSupergridding { get => _settings.EnableSupergridding; set => SetValue(ref _settings.EnableSupergridding, value); }
        
        [Torch.Views.Display(Description = "Enables unsafe impulses (experimental feature) for Piston block.", Name = "Enable Unsafe Piston Impulses", GroupName = "Experimental")]
        public bool EnableUnsafePistonImpulses { get => _settings.EnableUnsafePistonImpulses; set => SetValue(ref _settings.EnableUnsafePistonImpulses, value); }

        [Torch.Views.Display(Description = "Enables unsafe torques (experimental feature) for Rotor blocks.", Name = "Enable Unsafe Rotor Impulses", GroupName = "Experimental")]
        public bool EnableUnsafeRotorTorques { get => _settings.EnableUnsafeRotorTorques; set => SetValue(ref _settings.EnableUnsafeRotorTorques, value); }

        [Torch.Views.Display(Description = "Enables permanent death.", Name = "Permanent Death", GroupName = "Players")]
        public bool? PermanentDeath { get => _settings.PermanentDeath; set => SetValue(ref _settings.PermanentDeath, value); }

        [Torch.Views.Display(Description = "", Name = "Reset Ownership", GroupName = "Experimental")]
        public bool ResetOwnership { get => _settings.ResetOwnership; set => SetValue(ref _settings.ResetOwnership, value); }

        [Torch.Views.Display(Description = "By enabling this option grids will no longer turn dynamic when disconnected from static grids.", Name = "Unsupported Stations", GroupName = "Experimental")]
        public bool StationVoxelSupport { get => _settings.StationVoxelSupport; set => SetValue(ref _settings.StationVoxelSupport, value); }

        // This setting is not found in vanilla dedicated server GUI
        [Torch.Views.Display(Description = "Enables experimental mode.", Name = "Experimental Mode", GroupName = "Experimental")]
        public bool ExperimentalMode { get => _settings.ExperimentalMode; set => SetValue(ref _settings.ExperimentalMode, value); }


        [Torch.Views.Display(Description = "Enable radiation hazards. Requires Airtightness.", Name = "Enable Radiation", GroupName = "Environment")]
        public bool EnableRadiation { get => _settings.EnableRadiation; set => SetValue(ref _settings.EnableRadiation, value); }

        [Torch.Views.Display(Description = "A multiplier for the amount of radiation gained in space from sun exposure. Requires Airtightness and Radiation.", Name = "Solar Radiation Intensity", GroupName = "Environment")]
        public float SolarRadioationIntensity { get => _settings.SolarRadiationIntensity; set => SetValue(ref _settings.SolarRadiationIntensity, value); }


        [Torch.Views.Display(Description = "Enables reseting forageable items.", Name = "Reset forageable items", GroupName = "Trash Removal")]
        public bool ResetForageableItems { get => _settings.ResetForageableItems; set => SetValue(ref _settings.ResetForageableItems, value); }


        [Torch.Views.Display(Description = "Defines time in minutes after which forageable items are reset.", Name = "Reset forageable items time (min)", GroupName = "Trash Removal")]
        public int ResetForageableItemsTimeM { get => _settings.ResetForageableItemsTimeM; set => SetValue(ref _settings.ResetForageableItemsTimeM, value); }


        [Torch.Views.Display(Description = "Defines minimum distance from player for forageable items to reset.", Name = "Reset forageable items distance (m)", GroupName = "Trash Removal")]
        public int ResetForageableItemsDistance { get => _settings.ResetForageableItemsDistance; set => SetValue(ref _settings.ResetForageableItemsDistance, value); }


        [Torch.Views.Display(Description = "This value impacts how quickly the player becomes hungry, as well as how quickly food production occurs.", Name = "Food Consumption Rate", GroupName = "Environment")]
        public float FoodCunsumptionRate { get => _settings.FoodConsumptionRate; set => SetValue(ref _settings.FoodConsumptionRate, value); }


        [Torch.Views.Display(Description = "Enable buffs and enhancements which player characters earn over time. These benefits are lost if the player respawns.", Name = "Enable Survival Buffs", GroupName = "Environment")]
        public bool EnableSurvivalBuffs { get => _settings.EnableSurvivalBuffs; set => SetValue(ref _settings.EnableSurvivalBuffs, value); }


        [Torch.Views.Display(Description = "Upon respawning, all players will have their health, oxygen, gas tank levels, and other stats set to critically low levels.", Name = "Enable Reduced Stats", GroupName = "Environment")]
        public bool EnableReducedStatsOnRespawn { get => _settings.EnableReducedStatsOnRespawn; set => SetValue(ref _settings.EnableReducedStatsOnRespawn, value); }






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
