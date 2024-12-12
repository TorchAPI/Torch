using System;

namespace Torch.Utils.Reflected
{
    /// <summary>
    /// Indicates that the type will perform its own call to <see cref="ReflectedManager.Process(Type)"/>
    /// </summary>
    public class ReflectedLazyAttribute : Attribute
    {
    }
}
