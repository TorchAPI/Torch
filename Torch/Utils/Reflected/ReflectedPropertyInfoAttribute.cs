using System;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain the <see cref="System.Reflection.PropertyInfo"/> instance for the given property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedPropertyInfoAttribute : ReflectedMemberAttribute
    {
        /// <summary>
        /// Creates a reflected property info attribute using the given type and name.
        /// </summary>
        /// <param name="type">Type that contains the member</param>
        /// <param name="name">Name of the member</param>
        public ReflectedPropertyInfoAttribute(Type type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}