using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Torch.Server.ViewModels
{
    public class CheckpointViewModel : ViewModel
    {
        private MyObjectBuilder_Checkpoint _checkpoint;
        //private SessionSettingsViewModel _sessionSettings;

        public CheckpointViewModel(MyObjectBuilder_Checkpoint checkpoint)
        {
            _checkpoint = checkpoint;
            //_sessionSettings = new SessionSettingsViewModel(_checkpoint.Settings);
        }

        public static implicit operator MyObjectBuilder_Checkpoint(CheckpointViewModel model)
        {
            return model._checkpoint;
        }

        public Vector3I CurrentSector { get => _checkpoint.CurrentSector; set => SetValue(ref _checkpoint.CurrentSector, value); }

        public long ElapsedGameTime { get => _checkpoint.ElapsedGameTime; set => SetValue(ref _checkpoint.ElapsedGameTime, value); }

        public string SessionName { get => _checkpoint.SessionName; set => SetValue(ref _checkpoint.SessionName, value); }

        public MyPositionAndOrientation SpectatorPosition { get => _checkpoint.SpectatorPosition; set => SetValue(ref _checkpoint.SpectatorPosition, value); }

        public bool SpectatorIsLightOn { get => _checkpoint.SpectatorIsLightOn; set => SetValue(ref _checkpoint.SpectatorIsLightOn, value); }

        public MyCameraControllerEnum CameraController { get => _checkpoint.CameraController; set => SetValue(ref _checkpoint.CameraController, value); }

        public long CameraEntity { get => _checkpoint.CameraEntity; set => SetValue(ref _checkpoint.CameraEntity, value); }

        public long ControlledObject { get => _checkpoint.ControlledObject; set => SetValue(ref _checkpoint.ControlledObject, value); }

        public string Password { get => _checkpoint.Password; set => SetValue(ref _checkpoint.Password, value); }

        public string Description { get => _checkpoint.Description; set => SetValue(ref _checkpoint.Description, value); }

        public DateTime LastSaveTime { get => _checkpoint.LastSaveTime; set => SetValue(ref _checkpoint.LastSaveTime, value); }

        public float SpectatorDistance { get => _checkpoint.SpectatorDistance; set => SetValue(ref _checkpoint.SpectatorDistance, value); }

        public ulong? WorkshopId { get => _checkpoint.WorkshopId; set => SetValue(ref _checkpoint.WorkshopId, value); }

        public MyObjectBuilder_Toolbar CharacterToolbar { get => _checkpoint.CharacterToolbar; set => SetValue(ref _checkpoint.CharacterToolbar, value); }

        public SerializableDictionary<long, MyObjectBuilder_Checkpoint.PlayerId> ControlledEntities { get => _checkpoint.ControlledEntities; set => SetValue(ref _checkpoint.ControlledEntities, value); }

        //public SessionSettingsViewModel Settings
        //{
        //    get => _sessionSettings;
        //    set
        //    {
        //        SetValue(ref _sessionSettings, value);
        //        _checkpoint.Settings = _sessionSettings;
        //    }
        //}

        public MyObjectBuilder_ScriptManager ScriptManagerData => throw new NotImplementedException();

        public int AppVersion { get => _checkpoint.AppVersion; set => SetValue(ref _checkpoint.AppVersion, value); }

        public MyObjectBuilder_FactionCollection Factions => throw new NotImplementedException();

        //public List<MyObjectBuilder_Checkpoint.ModItem> Mods { get => _checkpoint.Mods; set => SetValue(ref _checkpoint.Mods, value); }

        public SerializableDictionary<ulong, MyPromoteLevel> PromotedUsers { get => _checkpoint.PromotedUsers; set => SetValue(ref _checkpoint.PromotedUsers, value); }

        public HashSet<ulong> CreativeTools { get => _checkpoint.CreativeTools; set => SetValue(ref _checkpoint.CreativeTools, value); }

        public SerializableDefinitionId Scenario { get => _checkpoint.Scenario; set => SetValue(ref _checkpoint.Scenario, value); }

        public List<MyObjectBuilder_Checkpoint.RespawnCooldownItem> RespawnCooldowns { get => _checkpoint.RespawnCooldowns; set => SetValue(ref _checkpoint.RespawnCooldowns, value); }

        public List<MyObjectBuilder_Identity> Identities { get => _checkpoint.Identities; set => SetValue(ref _checkpoint.Identities, value); }

        public List<MyObjectBuilder_Client> Clients { get => _checkpoint.Clients; set => SetValue(ref _checkpoint.Clients, value); }

        public MyEnvironmentHostilityEnum? PreviousEnvironmentHostility { get => _checkpoint.PreviousEnvironmentHostility; set => SetValue(ref _checkpoint.PreviousEnvironmentHostility, value); }

        public SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> AllPlayersData { get => _checkpoint.AllPlayersData; set => SetValue(ref _checkpoint.AllPlayersData, value); }

        public SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, List<Vector3>> AllPlayersColors { get => _checkpoint.AllPlayersColors; set => SetValue(ref _checkpoint.AllPlayersColors, value); }

        public List<MyObjectBuilder_ChatHistory> ChatHistory { get => _checkpoint.ChatHistory; set => SetValue(ref _checkpoint.ChatHistory, value); }

        public List<MyObjectBuilder_FactionChatHistory> FactionChatHistory { get => _checkpoint.FactionChatHistory; set => SetValue(ref _checkpoint.FactionChatHistory, value); }

        public List<long> NonPlayerIdentities { get => _checkpoint.NonPlayerIdentities; set => SetValue(ref _checkpoint.NonPlayerIdentities, value); }

        public SerializableDictionary<long, MyObjectBuilder_Gps> Gps { get => _checkpoint.Gps; set => SetValue(ref _checkpoint.Gps, value); }

        public SerializableBoundingBoxD? WorldBoundaries { get => _checkpoint.WorldBoundaries; set => SetValue(ref _checkpoint.WorldBoundaries, value); }

        public List<MyObjectBuilder_SessionComponent> SessionComponents { get => _checkpoint.SessionComponents; set => SetValue(ref _checkpoint.SessionComponents, value); }

        public SerializableDefinitionId GameDefinition { get => _checkpoint.GameDefinition; set => SetValue(ref _checkpoint.GameDefinition, value); }

        public HashSet<string> SessionComponentEnabled { get => _checkpoint.SessionComponentEnabled; set => SetValue(ref _checkpoint.SessionComponentEnabled, value); }

        public HashSet<string> SessionComponentDisabled { get => _checkpoint.SessionComponentDisabled; set => SetValue(ref _checkpoint.SessionComponentDisabled, value); }

        public DateTime InGameTime { get => _checkpoint.InGameTime; set => SetValue(ref _checkpoint.InGameTime, value); }

        public string CustomLoadingScreenText { get => _checkpoint.CustomLoadingScreenText; set => SetValue(ref _checkpoint.CustomLoadingScreenText, value); }

        public string CustomSkybox { get => _checkpoint.CustomSkybox; set => SetValue(ref _checkpoint.CustomSkybox, value); }

        public int RequiresDX { get => _checkpoint.RequiresDX; set => SetValue(ref _checkpoint.RequiresDX, value); }
    }
}
