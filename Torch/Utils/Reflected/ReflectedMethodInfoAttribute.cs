using System;
using System.Linq;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain the <see cref="System.Reflection.MethodInfo"/> instance for the given method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedMethodInfoAttribute : ReflectedMemberAttribute
    {
        /// <summary>
        /// Creates a reflected method info attribute using the given type and name.
        /// </summary>
        /// <param name="type">Type that contains the member</param>
        /// <param name="name">Name of the member</param>
        public ReflectedMethodInfoAttribute(Type type, string name)
        {
            Type = type;
            Name = name;
        }
        /// <summary>
        /// Expected parameters of this method, or null if any parameters are accepted.
        /// </summary>
        public Type[] Parameters { get; set; } = null;

        /// <summary>
        /// Assembly qualified names of <see cref="Parameters"/>
        /// </summary>
        public string[] ParameterNames
        {
            get => Parameters.Select(x => x.AssemblyQualifiedName).ToArray();
            set => Parameters = value?.Select(x => x == null ? null : Type.GetType(x)).ToArray();
        }

        /// <summary>
        /// Expected return type of this method, or null if any return type is accepted.
        /// </summary>
        public Type ReturnType { get; set; } = null;
    }
}