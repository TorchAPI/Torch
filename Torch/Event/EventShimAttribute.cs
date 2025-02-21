using System;

namespace Torch.Event
{
    /// <summary>
    /// Tagging class used to indicate that the class should be treated as an event shim.
    /// Only works for core assemblies loaded by Torch (non-plugins).
    /// </summary>
    /// <remarks>
    /// Event shims should be singleton, and have one (or more) fields that are of type <see cref="EventList{T}"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class EventShimAttribute : Attribute
    {
    }
}
