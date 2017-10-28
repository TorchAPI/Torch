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
        //TODO: these should be sorted sets for speed
        public MtObservableList<GridViewModel> Grids { get; set; } = new MtObservableList<GridViewModel>();
        public MtObservableList<CharacterViewModel> Characters { get; set; } = new MtObservableList<CharacterViewModel>();
        public MtObservableList<EntityViewModel> FloatingObjects { get; set; } = new MtObservableList<EntityViewModel>();
        public MtObservableList<VoxelMapViewModel> VoxelMaps { get; set; } = new MtObservableList<VoxelMapViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        private EntityViewModel _currentEntity;
        private UserControl _control;

        public EntityViewModel CurrentEntity
        {
            get => _currentEntity;
            set { _currentEntity = value; OnPropertyChanged(nameof(CurrentEntity)); }
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
            switch (obj)
            {
                case MyCubeGrid grid:
                    Grids.RemoveWhere(m => m.Id == grid.EntityId);
                    break;
                case MyCharacter character:
                    Characters.RemoveWhere(m => m.Id == character.EntityId);
                    break;
                case MyFloatingObject floating:
                    FloatingObjects.RemoveWhere(m => m.Id == floating.EntityId);
                    break;
                case MyVoxelBase voxel:
                    VoxelMaps.RemoveWhere(m => m.Id == voxel.EntityId);
                    break;
            }
        }

        private void MyEntities_OnEntityAdd(VRage.Game.Entity.MyEntity obj)
        {
            switch (obj)
            {
                case MyCubeGrid grid:
                    Grids.Add(new GridViewModel(grid, this));
                    break;
                case MyCharacter character:
                    Characters.Add(new CharacterViewModel(character, this));
                    break;
                case MyFloatingObject floating:
                    FloatingObjects.Add(new FloatingObjectViewModel(floating, this));
                    break;
                case MyVoxelBase voxel:
                    VoxelMaps.Add(new VoxelMapViewModel(voxel, this));
                    break;
            }
        }
    }
}
