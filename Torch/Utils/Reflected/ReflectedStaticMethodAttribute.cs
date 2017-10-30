using System;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain a delegate capable of invoking a static method.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// [ReflectedMethodAttribute(Type = typeof(Example)]
    /// private static Func<int, float, string> ExampleStatic;
    /// 
    /// private class Example {
    ///     private static int ExampleStatic(int a, float b) {
    ///         return a + ", " + b;
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedStaticMethodAttribute : ReflectedMethodAttribute
    {
    }
}