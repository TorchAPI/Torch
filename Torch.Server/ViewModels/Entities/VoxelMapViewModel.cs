using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Torch.Server.ViewModels.Entities
{
    public class VoxelMapViewModel : EntityViewModel
    {
        private MyVoxelBase Voxel => (MyVoxelBase)Entity;

        public override string Name => string.IsNullOrEmpty(Voxel.StorageName) ? "Unnamed" : Voxel.StorageName;

        public override bool CanStop => false;

        public MTObservableCollection<GridViewModel> AttachedGrids { get; } = new MTObservableCollection<GridViewModel>();

        public void UpdateAttachedGrids()
        {
            //TODO: fix
            return;

            AttachedGrids.Clear();
            var box = Entity.WorldAABB;
            var entities = new List<MyEntity>();
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, entities, MyEntityQueryType.Static);
            foreach (var entity in entities.Where(e => e is IMyCubeGrid))
            {
                var gridModel = Tree.Grids.FirstOrDefault(g => g.Entity.EntityId == entity.EntityId);
                if (gridModel == null)
                {
                    gridModel = new GridViewModel((MyCubeGrid)entity, Tree);
                    Tree.Grids.Add(gridModel);
                }

                AttachedGrids.Add(gridModel);
            }
        }

        public VoxelMapViewModel(MyVoxelBase e, EntityTreeViewModel tree) : base(e, tree)
        {

        }

        public VoxelMapViewModel()
        {
            
        }
    }
}
