using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Managers.EventManager
{
    public interface IEvent
    {
        /// <summary>
        /// An event that has been cancelled will no be processed in the default manner.
        /// </summary>
        /// <seealso cref="EventHandlerAttribute.SkipCancelled"/>
        bool Cancelled { get; }
    }
}
