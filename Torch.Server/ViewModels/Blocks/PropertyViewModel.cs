using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
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
            get
            {
                var val = default(T);
                TorchBase.Instance.InvokeBlocking(() => val = _prop.GetValue(Block.Block));
                return val;
            }
            set
            {
                TorchBase.Instance.InvokeBlocking(() => _prop.SetValue(Block.Block, value));
                OnPropertyChanged();
                Block.RefreshModel();
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
