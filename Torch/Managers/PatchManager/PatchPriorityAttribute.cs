using System;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Attribute used to decorate methods used for replacement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchPriorityAttribute : Attribute
    {
        /// <summary>
        /// <see cref="Priority"/>
        /// </summary>
        public PatchPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// The priority of this replacement.  A high priority prefix occurs first, and a high priority suffix or transpiler occurs last.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
