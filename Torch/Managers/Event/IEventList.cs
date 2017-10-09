using System.Reflection;
using Torch.API.Managers.Event;

namespace Torch.Managers.Event
{
    /// <summary>
    /// Represents the interface for adding and removing from an ordered list of callbacks.
    /// </summary>
    public interface IEventList
    {
        /// <summary>
        /// Adds an event handler for the given method, on the given instance.
        /// </summary>
        /// <param name="method">Handler method</param>
        /// <param name="instance">Instance to invoke the handler on</param>
        void AddHandler(MethodInfo method, IEventHandler instance);

        /// <summary>
        /// Removes all event handlers invoked on the given instance.
        /// </summary>
        /// <param name="instance">Instance to remove event handlers for</param>
        /// <returns>The number of event handlers removed</returns>
        int RemoveHandlers(IEventHandler instance);
    }
}
