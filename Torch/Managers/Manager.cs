using System;
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
            /// <summary>
            /// If this dependency isn't required.
            /// </summary>
            /// <remarks>
            /// The tagged field can be null if, and only if, this is true.
            /// </remarks>
            public bool Optional { get; set; } = false;

            /// <summary>
            /// Dependency must be loaded before and unloaded after the containing manager.
            /// </summary>
            /// <example>
            /// <code>
            /// public class NetworkManager : Manager { }
            /// public class ChatManager : Manager {
            ///     [Dependency(Ordered = true)]
            ///     private NetworkManager _network;
            /// }
            /// </code>
            /// Load order will be NetworkManager, then ChatManager.
            /// Unload order will be ChatManager, then NetworkManager
            /// </example>
            public bool Ordered { get; set; } = true;
        }

        protected ITorchBase Torch { get; }

        protected Manager(ITorchBase torchInstance)
        {
            Torch = torchInstance;
        }

        public virtual void Attach()
        {
            
        }

        public virtual void Detach()
        {
            
        }
    }
}
