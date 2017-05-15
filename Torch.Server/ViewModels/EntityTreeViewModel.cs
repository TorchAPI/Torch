using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Torch.Server.ViewModels.Entities;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Torch.Server.ViewModels
{
    public class EntityTreeViewModel : ViewModel
    {
        public MTObservableCollection<GridViewModel> Grids { get; set; } = new MTObservableCollection<GridViewModel>();
        public MTObservableCollection<CharacterViewModel> Characters { get; set; } = new MTObservableCollection<CharacterViewModel>();
        public MTObservableCollection<EntityViewModel> FloatingObjects { get; set; } = new MTObservableCollection<EntityViewModel>();
        public MTObservableCollection<VoxelMapViewModel> VoxelMaps { get; set; } = new MTObservableCollection<VoxelMapViewModel>();

        private EntityViewModel _currentEntity;

        public EntityViewModel CurrentEntity
        {
            get => _currentEntity;
            set { _currentEntity = value; OnPropertyChanged(); }
        }

        public EntityTreeViewModel()
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
            //TODO: make view models
            switch (obj)
            {
                case MyCubeGrid grid:
                    Grids.Add(new GridViewModel(grid));
                    break;
                case MyCharacter character:
                    Characters.Add(new CharacterViewModel(character));
                    break;
                case MyFloatingObject floating:
                    FloatingObjects.Add(new FloatingObjectViewModel(floating));
                    break;
                case MyVoxelBase voxel:
                    VoxelMaps.Add(new VoxelMapViewModel(voxel));
                    break;
            }
        }
    }
}
