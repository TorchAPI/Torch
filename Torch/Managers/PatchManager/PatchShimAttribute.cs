using System;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Tagging class used to indicate that the class should be treated as supplying patch rules.
    /// Only works for core assemblies loaded by Torch (non-plugins).
    /// </summary>
    /// <remarks>
    /// Patch shims should be singleton, and have one method of signature <i>void Patch(PatchContext)</i>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class PatchShimAttribute : Attribute
    {
    }
}
