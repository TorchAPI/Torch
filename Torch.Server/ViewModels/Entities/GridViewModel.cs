using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.Server.ViewModels.Blocks;

namespace Torch.Server.ViewModels.Entities
{
    public class GridViewModel : EntityViewModel, ILazyLoad
    {
        private MyCubeGrid Grid => (MyCubeGrid)Entity;
        public MTObservableCollection<BlockViewModel> Blocks { get; } = new MTObservableCollection<BlockViewModel>();
        private static readonly Logger Log = LogManager.GetLogger(nameof(GridViewModel));

        /// <inheritdoc />
        public string DescriptiveName => $"{Name} ({Grid.BlocksCount} blocks)";

        public GridViewModel() { }

        public GridViewModel(MyCubeGrid grid, EntityTreeViewModel tree) : base(grid, tree)
        {
            Log.Debug($"Creating model {Grid.DisplayName}");
            Blocks.Add(new BlockViewModel(null, Tree));
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
            var block = obj.FatBlock as IMyTerminalBlock;
            if (block != null)
                Blocks.Add(new BlockViewModel(block, Tree));

            Blocks.Sort(b => b.Block.GetType().AssemblyQualifiedName);
            OnPropertyChanged(nameof(Name));
        }

        private bool _load;
        public void Load()
        {
            if (_load)
                return;

            Log.Debug($"Loading model {Grid.DisplayName}");
            _load = true;
            Blocks.Clear();
            TorchBase.Instance.InvokeBlocking(() =>
            {
                foreach (var block in Grid.GetFatBlocks().Where(b => b is IMyTerminalBlock))
                {
                    Blocks.Add(new BlockViewModel((IMyTerminalBlock)block, Tree));
                }
            });
            Blocks.Sort(b => b.Block.GetType().AssemblyQualifiedName);

            Grid.OnBlockAdded += Grid_OnBlockAdded;
            Grid.OnBlockRemoved += Grid_OnBlockRemoved;
        }
    }
}
