using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.ViewModels.Blocks
{
    public class BlockViewModel : EntityViewModel
    {
        public IMyTerminalBlock Block { get; }
        public MTObservableCollection<PropertyViewModel> Properties { get; } = new MTObservableCollection<PropertyViewModel>();

        public string FullName => $"{Block.CubeGrid.CustomName} - {Block.CustomName}";

        public override string Name
        {
            get => Block?.CustomName ?? "null";
            set
            {
                TorchBase.Instance.InvokeBlocking(() => Block.CustomName = value); 
                OnPropertyChanged();
            }
        }

        public override bool CanStop => false;

        public BlockViewModel(IMyTerminalBlock block) : base(block)
        {
            Block = block;
            var propList = new List<ITerminalProperty>();
            block.GetProperties(propList);
            foreach (var prop in propList)
            {
                Type propType = null;
                foreach (var iface in prop.GetType().GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITerminalProperty<>))
                        propType = iface.GenericTypeArguments[0];
                }

                var modelType = typeof(PropertyViewModel<>).MakeGenericType(propType);
                Properties.Add((PropertyViewModel)Activator.CreateInstance(modelType, prop, this));
            }
        }

        public BlockViewModel()
        {
            
        }
    }
}
