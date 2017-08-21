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
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedGetterAttribute : Attribute, IReflectedFieldAttribute
    {
        public string Name { get; set; } = null;
        public Type Type { get; set; } = null;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedSetterAttribute : Attribute, IReflectedFieldAttribute
    {
        public string Name { get; set; } = null;
        public Type Type { get; set; } = null;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedMethodAttribute : Attribute
    {
        public string Name { get; set; } = null;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedStaticMethodAttribute : ReflectedMethodAttribute
    {
        public Type Type { get; set; }
    }

    public class ReflectionManager : Manager
    {
        /// <inheritdoc />
        public ReflectionManager(ITorchBase torchInstance) : base(torchInstance) { }

        public override void Attach()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                Process(asm);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

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
                ProcessInternal(t);
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

        private static void ProcessReflectedMethod(FieldInfo field, ReflectedMethodAttribute attr)
        {
            MethodInfo delegateMethod = field.FieldType.GetMethod("Invoke");
            ParameterInfo[] parameters = delegateMethod.GetParameters();
            Type trueType;
            Type[] trueParameterTypes;
            if (attr is ReflectedStaticMethodAttribute staticMethod)
            {
                trueType = staticMethod.Type;
                trueParameterTypes = parameters.Select(x => x.ParameterType).ToArray();
            }
            else
            {
                trueType = parameters[0].ParameterType;
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

        private static void ProcessInternal(Type t)
        {
            foreach (FieldInfo field in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = field.GetCustomAttribute<ReflectedMethodAttribute>();
                if (attr != null)
                    ProcessReflectedMethod(field, attr);
                var attr2 = field.GetCustomAttribute<ReflectedGetterAttribute>();
                if (attr2 != null)
                    ProcessReflectedField(field, attr2);
                var attr3 = field.GetCustomAttribute<ReflectedSetterAttribute>();
                if (attr3 != null)
                    ProcessReflectedField(field, attr3);

            }
        }

    }
}
