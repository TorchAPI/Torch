using System;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain a delegate capable of setting the value of a field.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// [ReflectedSetterAttribute(Name="_instanceField")]
    /// private static Action<Example, int> _instanceSetter;
    /// 
    /// [ReflectedSetterAttribute(Name="_staticField", Type=typeof(Example))]
    /// private static Action<int> _staticSetter;
    /// 
    /// private class Example {
    ///     private int _instanceField;
    ///     private static int _staticField;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedSetterAttribute : ReflectedMemberAttribute
    {
    }
}