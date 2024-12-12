using System;
using System.Windows.Controls;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Torch.Server.ViewModels.Entities;
using System.Windows.Threading;
using NLog;
using Torch.Collections;

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
        public MtObservableSortedDictionary<long, EntityViewModel> FloatingObjects { get; set; } = new MtObservableSortedDictionary<long, EntityViewModel>();
        public MtObservableSortedDictionary<long, VoxelMapViewModel> VoxelMaps { get; set; } = new MtObservableSortedDictionary<long, VoxelMapViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        public SortedView<GridViewModel> SortedGrids { get; }
        public SortedView<CharacterViewModel> SortedCharacters { get; }
        public SortedView<EntityViewModel> SortedFloatingObjects { get; }
        public SortedView<VoxelMapViewModel> SortedVoxelMaps { get; }

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

        // I hate you today WPF
        public EntityTreeViewModel() : this(null)
        {
        }

        public EntityTreeViewModel(UserControl control)
        {
            _control = control;
            var comparer = new EntityViewModel.Comparer(_currentSort);
            SortedGrids = new SortedView<GridViewModel>(Grids.Values, comparer);
            SortedCharacters = new SortedView<CharacterViewModel>(Characters.Values, comparer);
            SortedFloatingObjects = new SortedView<EntityViewModel>(FloatingObjects.Values, comparer);
            SortedVoxelMaps = new SortedView<VoxelMapViewModel>(VoxelMaps.Values, comparer);
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
