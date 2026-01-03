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
using Torch.Collections;
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
        public MtObservableSortedDictionary<long, GridViewModel> Grids { get; set; } = new MtObservableSortedDictionary<long, GridViewModel>();
        public MtObservableSortedDictionary<long, CharacterViewModel> Characters { get; set; } = new MtObservableSortedDictionary<long, CharacterViewModel>();
        public MtObservableSortedDictionary<long, FloatingObjectViewModel> FloatingObjects { get; set; } = new MtObservableSortedDictionary<long, FloatingObjectViewModel>();
        public MtObservableSortedDictionary<long, VoxelMapViewModel> VoxelMaps { get; set; } = new MtObservableSortedDictionary<long, VoxelMapViewModel>();
        public MtObservableSortedDictionary<long, PlayerViewModel> Players { get; set; } = new MtObservableSortedDictionary<long, PlayerViewModel>();
        public MtObservableSortedDictionary<long, FactionViewModel> Factions { get; set; } = new MtObservableSortedDictionary<long, FactionViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        public SortedView<GridViewModel> SortedGrids { get; }
        public SortedView<GridViewModel> FilteredSortedGrids { get; }
        public SortedView<CharacterViewModel> SortedCharacters { get; }
        public SortedView<FloatingObjectViewModel> SortedFloatingObjects { get; }
        public SortedView<VoxelMapViewModel> SortedVoxelMaps { get; }
        public SortedView<PlayerViewModel> SortedPlayers { get; }
        public SortedView<FactionViewModel> SortedFactions { get; }

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
            set => SetValue(ref _currentSort, value);
        }

        // Westin miller still hates you today WPF
        public EntityTreeViewModel() : this(null, null)
        {
        }

        public EntityTreeViewModel(UserControl control, ITorchServer server)
        {
            _control = control;
            var entityComparer = new EntityViewModel.Comparer(_currentSort);
            SortedGrids = new SortedView<GridViewModel>(Grids.Values, entityComparer);
            FilteredSortedGrids = new SortedView<GridViewModel>(Grids.Values, entityComparer);
            SortedCharacters = new SortedView<CharacterViewModel>(Characters.Values, entityComparer);
            SortedFloatingObjects = new SortedView<FloatingObjectViewModel>(FloatingObjects.Values, entityComparer);
            SortedVoxelMaps = new SortedView<VoxelMapViewModel>(VoxelMaps.Values, entityComparer);
            SortedPlayers = new SortedView<PlayerViewModel>(Players.Values, Comparer<PlayerViewModel>
                .Create((x, y) => 
                    string.Compare(x?.Name, y?.Name, StringComparison.InvariantCultureIgnoreCase))
            );
            SortedFactions = new SortedView<FactionViewModel>(Factions.Values, Comparer<FactionViewModel>
                .Create((x, y) =>
                    string.Compare(x?.Name, y?.Name, StringComparison.InvariantCultureIgnoreCase))
            );

            if (server != null)
            {
                var sessionManager = server.Managers.GetManager<ITorchSessionManager>();
                sessionManager.SessionStateChanged += RegisterLiveNonEntities;
            }
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
                        
                        Players.Add(new KeyValuePair<long, PlayerViewModel>(player.IdentityId, new PlayerViewModel(player, identity)));
                    }

                    foreach (MyFaction faction in MySession.Static.Factions.GetAllFactions())
                    {
                        Factions.Add(new KeyValuePair<long, FactionViewModel>(faction.FactionId, new FactionViewModel(faction)));
                    }

                    Sync.Players.RealPlayerIdentityCreated += NewPlayerCreated;
                    MySession.Static.Factions.FactionCreated += NewFactionCreated;
                    MySession.Static.Factions.FactionStateChanged += FactionChanged;
                    break;
                
                case TorchSessionState.Unloading:
                    Sync.Players.RealPlayerIdentityCreated -= NewPlayerCreated;
                    MySession.Static.Factions.FactionCreated -= NewFactionCreated;
                    Players.Clear();
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
                Factions.Add(new KeyValuePair<long, FactionViewModel>(faction.FactionId, new FactionViewModel(faction)));
            });
        }

        private void NewPlayerCreated(long identityId)
        {
            var player = MySession.Static.Players.TryGetPlayer(identityId);
            if (player is null) return;
            Players.Add(new KeyValuePair<long, PlayerViewModel>(player.Identity.IdentityId, new PlayerViewModel(player.Identity, new MyPlayer.PlayerId())));
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
