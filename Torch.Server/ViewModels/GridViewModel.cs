using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace Torch.Server.ViewModels
{
    public class GridViewModel : EntityViewModel
    {
        private MyCubeGrid Grid => (MyCubeGrid)Entity;

        /// <inheritdoc />
        public override string Name => $"{base.Name} ({Grid.BlocksCount} blocks)";

        public GridViewModel(MyCubeGrid grid) : base(grid)
        {

        }
    }
}
