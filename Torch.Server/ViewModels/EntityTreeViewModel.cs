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

namespace Torch.Server.ViewModels
{
    public class EntityTreeViewModel : ViewModel
    {
        //TODO: these should be sorted sets for speed
        public ObservableList<GridViewModel> Grids { get; set; } = new ObservableList<GridViewModel>();
        public ObservableList<CharacterViewModel> Characters { get; set; } = new ObservableList<CharacterViewModel>();
        public ObservableList<EntityViewModel> FloatingObjects { get; set; } = new ObservableList<EntityViewModel>();
        public ObservableList<VoxelMapViewModel> VoxelMaps { get; set; } = new ObservableList<VoxelMapViewModel>();
        public Dispatcher ControlDispatcher => _control.Dispatcher;

        private EntityViewModel _currentEntity;
        private UserControl _control;

        public EntityViewModel CurrentEntity
        {
            get => _currentEntity;
            set { _currentEntity = value; OnPropertyChanged(); }
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
                    Grids.Insert(new GridViewModel(grid, this), g => g.Name);
                    break;
                case MyCharacter character:
                    Characters.Insert(new CharacterViewModel(character, this), c => c.Name);
                    break;
                case MyFloatingObject floating:
                    FloatingObjects.Insert(new FloatingObjectViewModel(floating, this), f => f.Name);
                    break;
                case MyVoxelBase voxel:
                    VoxelMaps.Insert(new VoxelMapViewModel(voxel, this), v => v.Name);
                    break;
            }
        }
    }
}
