using System.Runtime.CompilerServices;
using Torch.API.Managers;

namespace Torch.API.Event
{
    /// <summary>
    /// Manager class responsible for registration of event handlers.
    /// </summary>
    public interface IEventManager : IManager
    {
        /// <summary>
        /// Registers all event handler methods contained in the given instance 
        /// </summary>
        /// <param name="handler">Instance to register</param>
        /// <returns><b>true</b> if added, <b>false</b> otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool RegisterHandler(IEventHandler handler);


        /// <summary>
        /// Unregisters all event handler methods contained in the given instance 
        /// </summary>
        /// <param name="handler">Instance to unregister</param>
        /// <returns><b>true</b> if removed, <b>false</b> otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool UnregisterHandler(IEventHandler handler);
    }
}
