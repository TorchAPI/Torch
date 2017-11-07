using System;

namespace Torch.Utils
{
    /// <summary>
    /// Indicates that this field should contain a delegate capable of retrieving the value of a field.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// [ReflectedGetterAttribute(Name="_instanceField")]
    /// private static Func<Example, int> _instanceGetter;
    /// 
    /// [ReflectedGetterAttribute(Name="_staticField", Type=typeof(Example))]
    /// private static Func<int> _staticGetter;
    /// 
    /// private class Example {
    ///     private int _instanceField;
    ///     private static int _staticField;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedGetterAttribute : ReflectedMemberAttribute
    {
    }
}