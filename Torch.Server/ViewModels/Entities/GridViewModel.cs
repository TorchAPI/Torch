using System;
using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Server.ViewModels.Blocks;

namespace Torch.Server.ViewModels.Entities
{
    public class GridViewModel : EntityViewModel, ILazyLoad
    {
        private MyCubeGrid Grid => (MyCubeGrid)Entity;
        public ObservableList<BlockViewModel> Blocks { get; } = new ObservableList<BlockViewModel>();

        /// <inheritdoc />
        public string DescriptiveName { get; }

        public GridViewModel() { }

        public GridViewModel(MyCubeGrid grid, EntityTreeViewModel tree) : base(grid, tree)
        {
            DescriptiveName = $"{grid.DisplayName} ({grid.BlocksCount} blocks)";
            Blocks.Add(new BlockViewModel(null, Tree));
        }

        private void Grid_OnBlockRemoved(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            if (obj.FatBlock != null)
                Blocks.RemoveWhere(b => b.Block.EntityId == obj.FatBlock?.EntityId);

            OnPropertyChanged(nameof(Name));
        }

        private void Grid_OnBlockAdded(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            var block = obj.FatBlock as IMyTerminalBlock;
            if (block != null)
                Blocks.Insert(new BlockViewModel(block, Tree), b => b.Name);

            OnPropertyChanged(nameof(Name));
        }

        private bool _load;
        public void Load()
        {
            if (_load)
                return;

            _load = true;
            Blocks.Clear();
            TorchBase.Instance.Invoke(() =>
            {
                foreach (var block in Grid.GetFatBlocks().Where(b => b is IMyTerminalBlock))
                {
                    Blocks.Add(new BlockViewModel((IMyTerminalBlock)block, Tree));
                }

                Grid.OnBlockAdded += Grid_OnBlockAdded;
                Grid.OnBlockRemoved += Grid_OnBlockRemoved;

                Tree.ControlDispatcher.BeginInvoke(() =>
                {
                    Blocks.Sort(b => b.Block.CustomName);
                });
            });
        }
    }
}
