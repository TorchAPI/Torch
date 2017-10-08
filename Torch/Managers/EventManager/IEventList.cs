using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Managers.EventManager
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
        void AddHandler(MethodInfo method, object instance);

        /// <summary>
        /// Removes all event handlers invoked on the given instance.
        /// </summary>
        /// <param name="instance">Instance to remove event handlers for</param>
        /// <returns>The number of event handlers removed</returns>
        int RemoveHandlers(object instance);
    }
}
