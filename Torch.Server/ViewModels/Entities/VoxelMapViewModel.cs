using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using System.Threading.Tasks;
using Torch.Collections;

namespace Torch.Server.ViewModels.Entities
{
    public class VoxelMapViewModel : EntityViewModel
    {
        private MyVoxelBase Voxel => (MyVoxelBase)Entity;

        public override string Name => string.IsNullOrEmpty(Voxel.StorageName) ? "UnnamedProcedural" : Voxel.StorageName;

        public override bool CanStop => false;

        public MtObservableList<GridViewModel> AttachedGrids { get; } = new MtObservableList<GridViewModel>();

        public async Task UpdateAttachedGrids()
        {
            //TODO: fix
            return;

            AttachedGrids.Clear();
            var box = Entity.WorldAABB;
            var entities = new List<MyEntity>();
            await TorchBase.Instance.InvokeAsync(() => MyEntities.GetTopMostEntitiesInBox(ref box, entities)).ConfigureAwait(false);
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
