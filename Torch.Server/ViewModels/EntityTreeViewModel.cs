using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Torch.Server.ViewModels.Entities;
using System.Windows.Threading;
using NLog;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Collections.Concurrent;
using VRage.Game.ModAPI;
using PlayerViewModel = Torch.Server.ViewModels.Entities.PlayerViewModel;

namespace Torch.Server.ViewModels
{
    public class EntityTreeViewModel : ViewModel
    {
        public enum SortEnum
        {
            Name,
            Size,
            Speed,
            Owner,
            BlockCount,
            DistFromCenter,
        }
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        //TODO: these should be sorted sets for speed
        public ObservableConcurrentDictionary<long, GridViewModel> Grids { get; set; } = new ObservableConcurrentDictionary<long, GridViewModel>();
        public ObservableConcurrentDictionary<long, CharacterViewModel> Characters { get; set; } = new ObservableConcurrentDictionary<long, CharacterViewModel>();
        public ObservableConcurrentDictionary<long, FloatingObjectViewModel> FloatingObjects { get; set; } = new ObservableConcurrentDictionary<long, FloatingObjectViewModel>();
        public ObservableConcurrentDictionary<long, VoxelMapViewModel> VoxelMaps { get; set; } = new ObservableConcurrentDictionary<long, VoxelMapViewModel>();
        public ObservableConcurrentDictionary<long, PlayerViewModel> Players { get; set; } = new ObservableConcurrentDictionary<long, PlayerViewModel>();
        public ObservableConcurrentDictionary<long, FactionViewModel> Factions { get; set; } = new ObservableConcurrentDictionary<long, FactionViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        public ObservableConcurrentSortedList<GridViewModel> SortedGrids { get; }
        public ObservableConcurrentSortedList<GridViewModel> FilteredSortedGrids { get; }
        public ObservableConcurrentSortedList<CharacterViewModel> SortedCharacters { get; }
        public ObservableConcurrentSortedList<FloatingObjectViewModel> SortedFloatingObjects { get; }
        public ObservableConcurrentSortedList<VoxelMapViewModel> SortedVoxelMaps { get; }
        public ObservableConcurrentSortedList<PlayerViewModel> SortedPlayers { get; }
        public ObservableConcurrentSortedList<FactionViewModel> SortedFactions { get; }

        private EntityViewModel _currentEntity;
        private SortEnum _currentSort;
        private UserControl _control;

        public EntityViewModel CurrentEntity
        {
            get => _currentEntity;
            set { _currentEntity = value; OnPropertyChanged(nameof(CurrentEntity)); }
        }

        public SortEnum CurrentSort
        {
            get => _currentSort;
            set
            {
                SetValue(ref _currentSort, value);
                UpdateSortComparer();
            }
        }

        // Westin miller still hates you today WPF
        public EntityTreeViewModel() : this(null, null)
        {
        }

        public EntityTreeViewModel(UserControl control, ITorchServer server)
        {
            _control = control;
            var entityComparer = new EntityViewModel.Comparer(_currentSort);
            SortedGrids = new ObservableConcurrentSortedList<GridViewModel>(Grids.Values, entityComparer);
            FilteredSortedGrids = new ObservableConcurrentSortedList<GridViewModel>(Grids.Values, entityComparer);
            SortedCharacters = new ObservableConcurrentSortedList<CharacterViewModel>(Characters.Values, entityComparer);
            SortedFloatingObjects = new ObservableConcurrentSortedList<FloatingObjectViewModel>(FloatingObjects.Values, entityComparer);
            SortedVoxelMaps = new ObservableConcurrentSortedList<VoxelMapViewModel>(VoxelMaps.Values, entityComparer);
            SortedPlayers = new ObservableConcurrentSortedList<PlayerViewModel>(Players.Values, Comparer<PlayerViewModel>
                .Create((x, y) => 
                    string.Compare(x?.Name, y?.Name, StringComparison.InvariantCultureIgnoreCase))
            );
            SortedFactions = new ObservableConcurrentSortedList<FactionViewModel>(Factions.Values, Comparer<FactionViewModel>
                .Create((x, y) =>
                    string.Compare(x?.Name, y?.Name, StringComparison.InvariantCultureIgnoreCase))
            );

            if (server != null)
            {
                var sessionManager = server.Managers.GetManager<ITorchSessionManager>();
                sessionManager.SessionStateChanged += RegisterLiveNonEntities;
            }
        }

        private void UpdateSortComparer()
        {
            var comparer = new EntityViewModel.Comparer(_currentSort);
            SortedGrids.SetComparer(comparer);
            FilteredSortedGrids.SetComparer(comparer);
            SortedCharacters.SetComparer(comparer);
            SortedFloatingObjects.SetComparer(comparer);
            SortedVoxelMaps.SetComparer(comparer);
        }

        private void RegisterLiveNonEntities(ITorchSession session, TorchSessionState newState)
        {
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    foreach (var identity in MySession.Static.Players.GetAllPlayers())
                    {
                        if (identity.SteamId == 0) continue;
                        var player = MySession.Static.Players.TryGetPlayerIdentity(identity.SteamId);
                        if (player is null) continue;
                        if (Players.ContainsKey(player.IdentityId)) continue;
                        
                        Players.Add(player.IdentityId, new PlayerViewModel(player, identity));
                    }

                    foreach (MyFaction faction in MySession.Static.Factions.GetAllFactions())
                    {
                        Factions.Add(faction.FactionId, new FactionViewModel(faction));
                    }

                    Sync.Players.RealPlayerIdentityCreated += NewPlayerCreated;
                    MySession.Static.Factions.FactionCreated += NewFactionCreated;
                    MySession.Static.Factions.FactionStateChanged += FactionChanged;
                    break;
                
                case TorchSessionState.Unloading:
                    Sync.Players.RealPlayerIdentityCreated -= NewPlayerCreated;
                    MySession.Static.Factions.FactionCreated -= NewFactionCreated;
                    MySession.Static.Factions.FactionStateChanged -= FactionChanged;
                    Players.Clear();
                    Factions.Clear();
                    break;
            }
        }

        // These might be off, but I only need the reason and main faction id.
        private void FactionChanged(MyFactionStateChange reason, long FactionId, long ToFactionId, long PlayerId, long SenderId)
        {
            switch (reason)
            {
                case MyFactionStateChange.RemoveFaction:
                    ControlDispatcher.Invoke(() => { Factions.Remove(FactionId);});
                    
                    break;
                case MyFactionStateChange.FactionMemberAcceptJoin:
                case MyFactionStateChange.FactionMemberPromote:
                case MyFactionStateChange.FactionMemberKick:
                case MyFactionStateChange.FactionMemberDemote:
                case MyFactionStateChange.FactionMemberLeave:
                    ControlDispatcher.Invoke(() =>
                    {
                        if (Factions.TryGetValue(FactionId, out FactionViewModel faction))
                            faction.GenerateMembers();
                    });
                    break;
            }
        }

        private void NewFactionCreated(long id)
        {
            ControlDispatcher.Invoke(() =>
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(id);
                if (faction is null) return;
                Factions.Add(faction.FactionId, new FactionViewModel(faction));
            });
        }

        private void NewPlayerCreated(long identityId)
        {
            var player = MySession.Static.Players.TryGetPlayer(identityId);
            if (player is null) return;
            Players.Add(player.Identity.IdentityId, new PlayerViewModel(player.Identity, player.Id));
        }

        public void Init()
        {
            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
            MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
        }

        private void MyEntities_OnEntityRemove(VRage.Game.Entity.MyEntity obj)
        {
            try
            {
                switch (obj)
                {
                    case MyCubeGrid grid:
                        Grids.Remove(grid.EntityId);
                        break;
                    case MyCharacter character:
                        Characters.Remove(character.EntityId);
                        break;
                    case MyFloatingObject floating:
                        FloatingObjects.Remove(floating.EntityId);
                        break;
                    case MyVoxelBase voxel:
                        if (voxel is MyPlanet || voxel is MyVoxelMap)
                        {
                            VoxelMaps.Remove(voxel.EntityId);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
                // ignore error "it's only UI"
            }
        }

        private void MyEntities_OnEntityAdd(VRage.Game.Entity.MyEntity obj)
        {
            try
            {
                switch (obj)
                {
                    case MyCubeGrid grid:
                        Grids.Add(grid.EntityId, new GridViewModel(grid, this));
                        break;
                    case MyCharacter character:
                        Characters.Add(character.EntityId, new CharacterViewModel(character, this));
                        break;
                    case MyFloatingObject floating:
                        FloatingObjects.Add(floating.EntityId, new FloatingObjectViewModel(floating, this));
                        break;
                    case MyVoxelBase voxel:
                        if (voxel is MyPlanet || voxel is MyVoxelMap)
                        {
                            VoxelMaps.Add(voxel.EntityId, new VoxelMapViewModel(voxel, this));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
                // ignore error "it's only UI"
            }
        }
    }
}
