using System;
using Sandbox.ModAPI.Interfaces;

namespace Torch.Server.ViewModels.Blocks
{
    public class PropertyViewModel<T> : PropertyViewModel
    {
        private readonly ITerminalProperty<T> _prop;
        public string Name { get; }
        public Type PropertyType => typeof(T);

        public T Value
        {
            get => _prop.GetValue(Block.Block);
            set
            {
                TorchBase.Instance.Invoke(() =>
                {
                    _prop.SetValue(Block.Block, value);
                    OnPropertyChanged();
                    Block.RefreshModel();
                });
            }
        }

        public PropertyViewModel(ITerminalProperty<T> property, BlockViewModel blockViewModel) : base(blockViewModel)
        {
            Name = property.Id;
            _prop = property;
        }
    }

    public class PropertyViewModel : ViewModel
    {
        protected readonly BlockViewModel Block;

        public PropertyViewModel(BlockViewModel blockViewModel)
        {
            Block = blockViewModel;
        }
    }
}
