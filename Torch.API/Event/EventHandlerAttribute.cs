using System;

namespace Torch.API.Event
{
    /// <summary>
    /// Attribute indicating that a method should be invoked when the event occurs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EventHandlerAttribute : Attribute
    {
        /// <summary>
        /// Events are executed from low priority to high priority.
        /// </summary>
        /// <remarks>
        /// While this may seem unintuitive this gives the high priority events the final say on changing/canceling events.
        /// </remarks>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Specifies if this handler should ignore a consumed event.
        /// </summary>
        /// <remarks>
        /// If <see cref="SkipCancelled"/> is <em>true</em> and the event is cancelled by a lower priority handler this handler won't be invoked.
        /// </remarks>
        /// <seealso cref="IEvent.Cancelled"/>
        public bool SkipCancelled { get; set; } = false;
    }
}
