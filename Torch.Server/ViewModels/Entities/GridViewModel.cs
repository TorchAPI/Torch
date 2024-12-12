using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.Collections;
using Torch.Server.ViewModels.Blocks;
using VRage.Game;

namespace Torch.Server.ViewModels.Entities
{
    public class GridViewModel : EntityViewModel, ILazyLoad
    {
        private static readonly MyCubeBlockDefinition _fillerDefinition = new MyCubeBlockDefinition()
        {
            Id = new MyDefinitionId(typeof(MyObjectBuilder_DefinitionBase), "")
        };

        private class CubeBlockDefinitionComparer : IComparer<MyCubeBlockDefinition>
        {
            public static readonly CubeBlockDefinitionComparer Default = new CubeBlockDefinitionComparer();

            public int Compare(MyCubeBlockDefinition x, MyCubeBlockDefinition y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;
                if (ReferenceEquals(x, y))
                    return 0;
                MyDefinitionId xi = x.Id;
                MyDefinitionId yi = y.Id;
                if (xi == yi)
                    return 0;
                if (xi.TypeId != yi.TypeId)
                    return string.CompareOrdinal(((Type) xi.TypeId).Name, ((Type) yi.TypeId).Name);
                return xi.SubtypeId == yi.SubtypeId ? 0 : string.CompareOrdinal(xi.SubtypeName, yi.SubtypeName);
            }
        }

        private MyCubeGrid Grid => (MyCubeGrid) Entity;

        public MtObservableSortedDictionary<MyCubeBlockDefinition, MtObservableSortedDictionary<long, BlockViewModel>>
            Blocks { get; } =
            new MtObservableSortedDictionary<MyCubeBlockDefinition, MtObservableSortedDictionary<long, BlockViewModel>>(
                CubeBlockDefinitionComparer.Default);
        
        public GridViewModel()
        {
        }

        public GridViewModel(MyCubeGrid grid, EntityTreeViewModel tree) : base(grid, tree)
        {
            //DescriptiveName = $"{grid.DisplayName} ({grid.BlocksCount} blocks)";
            Blocks.Add(_fillerDefinition, new MtObservableSortedDictionary<long, BlockViewModel>());
        }

        private void Grid_OnBlockRemoved(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            if (obj.FatBlock != null)
                RemoveBlock(obj.FatBlock);

            OnPropertyChanged(nameof(Name));
        }

        private void RemoveBlock(MyCubeBlock block)
        {
            if (!Blocks.TryGetValue(block.BlockDefinition, out var group))
                return;
            if (group.Remove(block.EntityId) && group.Count == 0 && Blocks.Count > 1)
                Blocks.Remove(block.BlockDefinition);
        }

        private void AddBlock(MyTerminalBlock block)
        {
            if (!Blocks.TryGetValue(block.BlockDefinition, out var group))
                group = Blocks[block.BlockDefinition] = new MtObservableSortedDictionary<long, BlockViewModel>();
            group.Add(block.EntityId, new BlockViewModel(block, Tree));
        }

        private void Grid_OnBlockAdded(Sandbox.Game.Entities.Cube.MySlimBlock obj)
        {
            var block = obj.FatBlock as MyTerminalBlock;
            if (block != null)
                AddBlock(block);

            OnPropertyChanged(nameof(Name));
        }

        private bool _load;

        public void Load()
        {
            if (_load)
                return;

            _load = true;
            TorchBase.Instance.Invoke(() =>
            {
                Blocks.Clear();
                foreach (var block in Grid.GetFatBlocks().OfType<MyTerminalBlock>())
                    AddBlock(block);

                Grid.OnBlockAdded += Grid_OnBlockAdded;
                Grid.OnBlockRemoved += Grid_OnBlockRemoved;
            });
        }
    }
}