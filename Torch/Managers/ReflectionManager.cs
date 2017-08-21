using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Torch.API;

namespace Torch.Managers
{
    internal interface IReflectedFieldAttribute
    {
        string Name { get; set; }
        Type Type { get; set; }
    }

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
    public class ReflectedGetterAttribute : Attribute, IReflectedFieldAttribute
    {
        /// <summary>
        /// Name of the field to get.  If null, the tagged field's name.
        /// </summary>
        public string Name { get; set; } = null;
        /// <summary>
        /// Declaring type of the field to get.  If null, inferred from the instance argument type.
        /// </summary>
        public Type Type { get; set; } = null;
    }

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
    public class ReflectedSetterAttribute : Attribute, IReflectedFieldAttribute
    {
        /// <summary>
        /// Name of the field to get.  If null, the tagged field's name.
        /// </summary>
        public string Name { get; set; } = null;
        /// <summary>
        /// Declaring type of the field to get.  If null, inferred from the instance argument type.
        /// </summary>
        public Type Type { get; set; } = null;
    }

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
    public class ReflectedMethodAttribute : Attribute
    {
        /// <summary>
        /// Name of the method to invoke.  If null, the tagged field's name.
        /// </summary>
        public string Name { get; set; } = null;
        /// <summary>
        /// Declaring type of the method to invoke.  If null, inferred from the instance argument type.
        /// </summary>
        public Type Type { get; set; } = null;
    }

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

    /// <summary>
    /// Automatically calls <see cref="ReflectionManager.Process(Assembly)"/> for every assembly already loaded, and every assembly that is loaded in the future.
    /// </summary>
    public class ReflectionManager : Manager
    {
        private static readonly string[] _namespaceBlacklist = new[] {
            "System", "VRage", "Sandbox", "SpaceEngineers"
        };

        /// <inheritdoc />
        public ReflectionManager(ITorchBase torchInstance) : base(torchInstance) { }

        /// <inheritdoc />
        public override void Attach()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                Process(asm);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        /// <inheritdoc />
        public override void Detach()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Process(args.LoadedAssembly);
        }

        private static readonly HashSet<Type> _processedTypes = new HashSet<Type>();

        /// <summary>
        /// Ensures all reflected fields and methods contained in the given type are initialized
        /// </summary>
        /// <param name="t">Type to process</param>
        public static void Process(Type t)
        {
            if (_processedTypes.Add(t))
            {
                foreach (string ns in _namespaceBlacklist)
                    if (t.FullName.StartsWith(ns))
                        return;
                foreach (FieldInfo field in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    Process(field);
            }
        }

        /// <summary>
        /// Ensures all types in the given assembly are initialized using <see cref="Process(Type)"/>
        /// </summary>
        /// <param name="asm">Assembly to process</param>
        public static void Process(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
                Process(type);
        }

        /// <summary>
        /// Processes the given field, determines if it's reflected, and initializes it if it is.
        /// </summary>
        /// <param name="field">Field to process</param>
        /// <returns>true if it was reflected, false if it wasn't reflectable</returns>
        /// <exception cref="ArgumentException">If the field failed to process</exception>
        public static bool Process(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<ReflectedMethodAttribute>();
            if (attr != null)
            {
                ProcessReflectedMethod(field, attr);
                return true;
            }
            var attr2 = field.GetCustomAttribute<ReflectedGetterAttribute>();
            if (attr2 != null)
            {
                ProcessReflectedField(field, attr2);
                return true;
            }
            var attr3 = field.GetCustomAttribute<ReflectedSetterAttribute>();
            if (attr3 != null)
            {
                ProcessReflectedField(field, attr3);
                return true;
            }

            return false;
        }

        private static void ProcessReflectedMethod(FieldInfo field, ReflectedMethodAttribute attr)
        {
            MethodInfo delegateMethod = field.FieldType.GetMethod("Invoke");
            ParameterInfo[] parameters = delegateMethod.GetParameters();
            Type trueType = attr.Type;
            Type[] trueParameterTypes;
            if (attr is ReflectedStaticMethodAttribute)
            {
                trueParameterTypes = parameters.Select(x => x.ParameterType).ToArray();
            }
            else
            {
                trueType = trueType ?? parameters[0].ParameterType;
                trueParameterTypes = parameters.Skip(1).Select(x => x.ParameterType).ToArray();
            }

            MethodInfo methodInstance = trueType.GetMethod(attr.Name ?? field.Name,
                (attr is ReflectedStaticMethodAttribute ? BindingFlags.Static : BindingFlags.Instance) |
                BindingFlags.Public |
                BindingFlags.NonPublic, null, CallingConventions.Any, trueParameterTypes, null);

            if (attr is ReflectedStaticMethodAttribute)
                field.SetValue(null, Delegate.CreateDelegate(field.FieldType, methodInstance));
            else
            {
                ParameterExpression[] paramExp = parameters.Select(x => Expression.Parameter(x.ParameterType)).ToArray();
                field.SetValue(null,
                    Expression.Lambda(Expression.Call(paramExp[0], methodInstance, paramExp.Skip(1)), paramExp)
                              .Compile());
            }
        }

        private static T GetFieldPropRecursive<T>(Type baseType, string name, BindingFlags flags, Func<Type, string, BindingFlags, T> getter) where T : class
        {
            while (baseType != null)
            {
                T result = getter.Invoke(baseType, name, flags);
                if (result != null)
                    return result;

                baseType = baseType.BaseType;
            }

            return null;
        }

        private static void ProcessReflectedField(FieldInfo field, IReflectedFieldAttribute attr)
        {
            MethodInfo delegateMethod = field.FieldType.GetMethod("Invoke");
            ParameterInfo[] parameters = delegateMethod.GetParameters();
            string trueName = attr.Name ?? field.Name;
            Type trueType = attr.Type;
            bool isStatic;
            if (attr is ReflectedSetterAttribute)
            {
                if (delegateMethod.ReturnType != typeof(void) || (parameters.Length != 1 && parameters.Length != 2))
                    throw new ArgumentOutOfRangeException(nameof(field),
                        "Delegate for setter must be an action with one or two arguments");

                isStatic = parameters.Length == 1;
                if (trueType == null && isStatic)
                    throw new ArgumentException("Static field setters need their type defined", nameof(field));

                if (!isStatic)
                    trueType = parameters[0].ParameterType;
            }
            else if (attr is ReflectedGetterAttribute)
            {
                if (delegateMethod.ReturnType == typeof(void) || (parameters.Length != 0 && parameters.Length != 1))
                    throw new ArgumentOutOfRangeException(nameof(field),
                        "Delegate for getter must be an function with one or no arguments");

                isStatic = parameters.Length == 0;
                if (trueType == null && isStatic)
                    throw new ArgumentException("Static field getters need their type defined", nameof(field));

                if (!isStatic)
                    trueType = parameters[0].ParameterType;
            }
            else
                throw new ArgumentException($"Field attribute type {attr.GetType().FullName} is invalid", nameof(field));

            BindingFlags bindingFlags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Public;
            FieldInfo sourceField = GetFieldPropRecursive(trueType, trueName, bindingFlags,
                (a, b, c) => a.GetField(b, c));
            PropertyInfo sourceProperty =
                GetFieldPropRecursive(trueType, trueName, bindingFlags, (a, b, c) => a.GetProperty(b, c));
            if (sourceField == null && sourceProperty == null)
                throw new ArgumentException(
                    $"Unable to find field or property for {trueName} in {trueType} or its base types", nameof(field));

            ParameterExpression[] paramExp = parameters.Select(x => Expression.Parameter(x.ParameterType)).ToArray();

            MemberExpression fieldExp = sourceField != null
                                            ? Expression.Field(isStatic ? null : paramExp[0], sourceField)
                                            : Expression.Property(isStatic ? null : paramExp[0], sourceProperty);
            Expression impl;
            if (attr is ReflectedSetterAttribute)
            {
                impl = Expression.Block(Expression.Assign(fieldExp, paramExp[isStatic ? 0 : 1]), Expression.Default(typeof(void)));
            }
            else
            {
                impl = fieldExp;
            }

            field.SetValue(null, Expression.Lambda(impl, paramExp).Compile());
        }
    }
}
