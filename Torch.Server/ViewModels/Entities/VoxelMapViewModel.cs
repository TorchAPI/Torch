using Sandbox.Game.Entities;

namespace Torch.Server.ViewModels.Entities
{
    public class VoxelMapViewModel : EntityViewModel
    {
        private MyVoxelBase Voxel => (MyVoxelBase)Entity;

        public override string Name => Voxel.StorageName;

        public override bool CanStop => false;

        public VoxelMapViewModel(MyVoxelBase e) : base(e) { }
    }
}
