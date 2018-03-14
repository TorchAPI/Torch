using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Torch.Server.ViewModels.Entities;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using System.Windows.Threading;
using NLog;
using Torch.Collections;

namespace Torch.Server.ViewModels
{
    public class EntityTreeViewModel : ViewModel
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        //TODO: these should be sorted sets for speed
        public MtObservableSortedDictionary<long, GridViewModel> Grids { get; set; } = new MtObservableSortedDictionary<long, GridViewModel>();
        public MtObservableSortedDictionary<long, CharacterViewModel> Characters { get; set; } = new MtObservableSortedDictionary<long, CharacterViewModel>();
        public MtObservableSortedDictionary<long, EntityViewModel> FloatingObjects { get; set; } = new MtObservableSortedDictionary<long, EntityViewModel>();
        public MtObservableSortedDictionary<long, VoxelMapViewModel> VoxelMaps { get; set; } = new MtObservableSortedDictionary<long, VoxelMapViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        private EntityViewModel _currentEntity;
        private UserControl _control;

        public EntityViewModel CurrentEntity
        {
            get => _currentEntity;
            set { _currentEntity = value; OnPropertyChanged(nameof(CurrentEntity)); }
        }

        // I hate you today WPF
        public EntityTreeViewModel() : this(null)
        {
        }

        public EntityTreeViewModel(UserControl control)
        {
            _control = control;
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
                        VoxelMaps.Remove(voxel.EntityId);
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
                        Grids.Add(obj.EntityId, new GridViewModel(grid, this));
                        break;
                    case MyCharacter character:
                        Characters.Add(obj.EntityId, new CharacterViewModel(character, this));
                        break;
                    case MyFloatingObject floating:
                        FloatingObjects.Add(obj.EntityId, new FloatingObjectViewModel(floating, this));
                        break;
                    case MyVoxelBase voxel:
                        VoxelMaps.Add(obj.EntityId, new VoxelMapViewModel(voxel, this));
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
