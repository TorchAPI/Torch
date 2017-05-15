using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.Server.ViewModels.Blocks;

namespace Torch.Server.ViewModels.Entities
{
    public class GridViewModel : EntityViewModel
    {
        private MyCubeGrid Grid => (MyCubeGrid)Entity;
        public MTObservableCollection<BlockViewModel> Blocks { get; } = new MTObservableCollection<BlockViewModel>();

        /// <inheritdoc />
        public override string Name => $"{base.Name} ({Grid.BlocksCount} blocks)";

        public GridViewModel(MyCubeGrid grid) : base(grid)
        {
            TorchBase.Instance.InvokeBlocking(() =>
            {
                foreach (var block in grid.GetFatBlocks().Where(b => b is IMyTerminalBlock))
                {
                    Blocks.Add(new BlockViewModel((IMyTerminalBlock)block));
                }
            });
            Blocks.Sort(b => b.Block.GetType().AssemblyQualifiedName);

            grid.OnBlockAdded += Grid_OnBlockAdded;
            grid.OnBlockRemoved += Grid_OnBlockRemoved;
        }

        private void Grid_OnBlockRemoved(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            if (obj.FatBlock != null)
                Blocks.RemoveWhere(b => b.Block.EntityId == obj.FatBlock?.EntityId);

            Blocks.Sort(b => b.Block.GetType().AssemblyQualifiedName);
            OnPropertyChanged(nameof(Name));
        }

        private void Grid_OnBlockAdded(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            if (obj.FatBlock != null)
                Blocks.Add(new BlockViewModel((IMyTerminalBlock)obj.FatBlock));

            Blocks.Sort(b => b.Block.GetType().AssemblyQualifiedName);
            OnPropertyChanged(nameof(Name));
        }
    }
}
