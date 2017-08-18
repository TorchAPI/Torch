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
        /// <summary>
        /// Indicates a field is a dependency of this parent manager.
        /// </summary>
        /// <example>
        /// <code>
        /// public class NetworkManager : Manager { }
        /// public class ChatManager : Manager {
        ///     [Dependency(Optional = false)]
        ///     private NetworkManager _network;
        /// }
        /// </code>
        /// </example>
        [AttributeUsage(AttributeTargets.Field)]
        public class DependencyAttribute : Attribute
        {
            public bool Optional { get; set; } = false;
        }

        protected ITorchBase Torch { get; }

        protected Manager(ITorchBase torchInstance)
        {
            Torch = torchInstance;
        }

        public virtual void Init()
        {
            
        }

        public virtual void Dispose()
        {
            
        }
    }
}
