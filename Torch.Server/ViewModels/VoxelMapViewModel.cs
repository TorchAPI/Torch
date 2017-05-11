using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Medieval.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using VRage.ModAPI;

namespace Torch.Server.ViewModels
{
    public class VoxelMapViewModel : EntityViewModel
    {
        private MyVoxelBase Voxel => (MyVoxelBase)Entity;

        public override string Name => Voxel.StorageName;

        public override bool CanStop => false;

        public VoxelMapViewModel(MyVoxelBase e) : base(e) { }
    }
}
