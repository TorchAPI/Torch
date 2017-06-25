using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;

namespace Torch.Managers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagerAttribute : Attribute
    {
        
    }

    public abstract class Manager : IManager
    {
        protected ITorchBase Torch { get; }

        protected Manager(ITorchBase torchInstance)
        {
            Torch = torchInstance;
        }

        public virtual void Init()
        {
            
        }
    }
}
