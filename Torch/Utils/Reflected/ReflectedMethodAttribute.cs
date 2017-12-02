using System;
using System.Linq;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain a delegate capable of invoking an instance method.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// [ReflectedMethodAttribute]
    /// private static Func<Example, int, float, string> ExampleInstance;
    /// 
    /// private class Example {
    ///     private int ExampleInstance(int a, float b) {
    ///         return a + ", " + b;
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedMethodAttribute : ReflectedMemberAttribute
    {
        /// <summary>
        /// When set the parameters types for the method are assumed to be this.
        /// </summary>
        public Type[] OverrideTypes { get; set; }

        /// <summary>
        /// Assembly qualified names of <see cref="OverrideTypes"/>
        /// </summary>
        public string[] OverrideTypeNames
        {
            get => OverrideTypes.Select(x => x.AssemblyQualifiedName).ToArray();
            set => OverrideTypes = value?.Select(x => x == null ? null : Type.GetType(x)).ToArray();
        }
    }
}