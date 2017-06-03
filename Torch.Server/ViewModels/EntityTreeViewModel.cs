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
            switch (obj)
            {
                case MyCubeGrid grid:
                    if (Grids.All(g => g.Entity.EntityId != obj.EntityId))
                        Grids.Add(new GridViewModel(grid, this));
                    break;
                case MyCharacter character:
                    if (Characters.All(g => g.Entity.EntityId != obj.EntityId))
                        Characters.Add(new CharacterViewModel(character, this));
                    break;
                case MyFloatingObject floating:
                    if (FloatingObjects.All(g => g.Entity.EntityId != obj.EntityId))
                        FloatingObjects.Add(new FloatingObjectViewModel(floating, this));
                    break;
                case MyVoxelBase voxel:
                    if (VoxelMaps.All(g => g.Entity.EntityId != obj.EntityId))
                        VoxelMaps.Add(new VoxelMapViewModel(voxel, this));
                    break;
            }
        }
    }
}
