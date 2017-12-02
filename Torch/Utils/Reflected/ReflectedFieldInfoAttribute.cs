using System;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain the <see cref="System.Reflection.FieldInfo"/> instance for the given field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedFieldInfoAttribute : ReflectedMemberAttribute
    {
        /// <summary>
        /// Creates a reflected field info attribute using the given type and name.
        /// </summary>
        /// <param name="type">Type that contains the member</param>
        /// <param name="name">Name of the member</param>
        public ReflectedFieldInfoAttribute(Type type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}