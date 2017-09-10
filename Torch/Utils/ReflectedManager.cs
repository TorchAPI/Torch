using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Torch.API;

namespace Torch.Utils
{
    public abstract class ReflectedMemberAttribute : Attribute
    {
        /// <summary>
        /// Name of the member to access.  If null, the tagged field's name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Declaring type of the member to access.  If null, inferred from the instance argument type.
        /// </summary>
        public Type Type { get; set; } = null;

        /// <summary>
        /// Assembly qualified name of <see cref="Type"/>
        /// </summary>
        public string TypeName
        {
            get => Type?.AssemblyQualifiedName;
            set => Type = value == null ? null : Type.GetType(value, true);
        }
    }

    #region MemberInfoAttributes
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
        /// Expected return type of this method, or null if any return type is accepted.
        /// </summary>
        public Type ReturnType { get; set; } = null;
    }

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
    #endregion

    #region FieldPropGetSet
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
    #endregion

    #region Invoker
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
    #endregion

    #region EventReplacer
    /// <summary>
    /// Instance of statefully replacing and restoring the callbacks of an event.
    /// </summary>
    public class ReflectedEventReplacer
    {
        private const BindingFlags BindFlagAll = BindingFlags.Static |
                                                 BindingFlags.Instance |
                                                 BindingFlags.Public |
                                                 BindingFlags.NonPublic;

        private object _instance;
        private Func<IEnumerable<Delegate>> _backingStoreReader;
        private Action<Delegate> _callbackAdder;
        private Action<Delegate> _callbackRemover;
        private readonly ReflectedEventReplaceAttribute _attributes;
        private readonly HashSet<Delegate> _registeredCallbacks = new HashSet<Delegate>();
        private readonly MethodInfo _targetMethodInfo;

        internal ReflectedEventReplacer(ReflectedEventReplaceAttribute attr)
        {
            _attributes = attr;
            FieldInfo backingStore = GetEventBackingField(attr.EventName, attr.EventDeclaringType);
            if (backingStore == null)
                throw new ArgumentException($"Unable to find backing field for event {attr.EventDeclaringType.FullName}#{attr.EventName}");
            EventInfo evtInfo = ReflectedManager.GetFieldPropRecursive(attr.EventDeclaringType, attr.EventName, BindFlagAll, (a, b, c) => a.GetEvent(b, c));
            if (evtInfo == null)
                throw new ArgumentException($"Unable to find event info for event {attr.EventDeclaringType.FullName}#{attr.EventName}");
            _backingStoreReader = () => GetEventsInternal(_instance, backingStore);
            _callbackAdder = (x) => evtInfo.AddEventHandler(_instance, x);
            _callbackRemover = (x) => evtInfo.RemoveEventHandler(_instance, x);
            if (attr.TargetParameters == null)
            {
                _targetMethodInfo = attr.TargetDeclaringType.GetMethod(attr.TargetName, BindFlagAll);
                if (_targetMethodInfo == null)
                    throw new ArgumentException($"Unable to find method {attr.TargetDeclaringType.FullName}#{attr.TargetName} to replace");
            }
            else
            {
                _targetMethodInfo =
                    attr.TargetDeclaringType.GetMethod(attr.TargetName, BindFlagAll, null, attr.TargetParameters, null);
                if (_targetMethodInfo == null)
                    throw new ArgumentException($"Unable to find method {attr.TargetDeclaringType.FullName}#{attr.TargetName}){string.Join(", ", attr.TargetParameters.Select(x => x.FullName))}) to replace");
            }
        }

        /// <summary>
        /// Test that this replacement can be performed.
        /// </summary>
        /// <param name="instance">The instance to operate on, or null if static</param>
        /// <returns>true if possible, false if unsuccessful</returns>
        public bool Test(object instance)
        {
            _instance = instance;
            _registeredCallbacks.Clear();
            foreach (Delegate callback in _backingStoreReader.Invoke())
                if (callback.Method == _targetMethodInfo)
                    _registeredCallbacks.Add(callback);

            return _registeredCallbacks.Count > 0;
        }

        private Delegate _newCallback;

        /// <summary>
        /// Removes the target callback defined in the attribute and replaces it with the provided callback.
        /// </summary>
        /// <param name="newCallback">The new event callback</param>
        /// <param name="instance">The instance to operate on, or null if static</param>
        public void Replace(Delegate newCallback, object instance)
        {
            _instance = instance;
            if (_newCallback != null)
                throw new Exception("Reflected event replacer is in invalid state:  Replace when already replaced");
            _newCallback = newCallback;
            Test(instance);
            if (_registeredCallbacks.Count == 0)
                throw new Exception("Reflected event replacer is in invalid state:  Nothing to replace");
            foreach (Delegate callback in _registeredCallbacks)
                _callbackRemover.Invoke(callback);
            _callbackAdder.Invoke(_newCallback);
        }

        /// <summary>
        /// Checks if the callback is currently replaced
        /// </summary>
        public bool Replaced => _newCallback != null;

        /// <summary>
        /// Removes the callback added by <see cref="Replace"/> and puts the original callback back.
        /// </summary>
        /// <param name="instance">The instance to operate on, or null if static</param>
        public void Restore(object instance)
        {
            _instance = instance;
            if (_newCallback == null)
                throw new Exception("Reflected event replacer is in invalid state:  Restore when not replaced");
            _callbackRemover.Invoke(_newCallback);
            foreach (Delegate callback in _registeredCallbacks)
                _callbackAdder.Invoke(callback);
            _newCallback = null;
        }


        private static readonly string[] _backingFieldForEvent = { "{0}", "<backing_store>{0}" };

        private static FieldInfo GetEventBackingField(string eventName, Type baseType)
        {
            FieldInfo eventField = null;
            Type type = baseType;
            while (type != null && eventField == null)
            {
                for (var i = 0; i < _backingFieldForEvent.Length && eventField == null; i++)
                    eventField = type.GetField(string.Format(_backingFieldForEvent[i], eventName), BindFlagAll);
                type = type.BaseType;
            }
            return eventField;
        }

        private static IEnumerable<Delegate> GetEventsInternal(object instance, FieldInfo eventField)
        {
            if (eventField.GetValue(instance) is MulticastDelegate eventDel)
            {
                foreach (Delegate handle in eventDel.GetInvocationList())
                    yield return handle;
            }
        }
    }

    /// <summary>
    /// Attribute used to indicate that the the given field, of type <![CDATA[Func<ReflectedEventReplacer>]]>, should be filled with
    /// a function used to create a new event replacer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedEventReplaceAttribute : Attribute
    {
        /// <summary>
        /// Type that the event is declared in
        /// </summary>
        public Type EventDeclaringType { get; set; }
        /// <summary>
        /// Name of the event
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Type that the method to replace is declared in
        /// </summary>
        public Type TargetDeclaringType { get; set; }
        /// <summary>
        /// Name of the method to replace
        /// </summary>
        public string TargetName { get; set; }
        /// <summary>
        /// Optional parameters of the method to replace.  Null to ignore.
        /// </summary>
        public Type[] TargetParameters { get; set; } = null;

        /// <summary>
        /// Creates a reflected event replacer attribute to, for the event defined as eventName in eventDeclaringType,
        /// replace the method defined as targetName in targetDeclaringType with a custom callback.
        /// </summary>
        /// <param name="eventDeclaringType">Type the event is declared in</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="targetDeclaringType">Type the method to remove is declared in</param>
        /// <param name="targetName">Name of the method to remove</param>
        public ReflectedEventReplaceAttribute(Type eventDeclaringType, string eventName, Type targetDeclaringType,
            string targetName)
        {
            EventDeclaringType = eventDeclaringType;
            EventName = eventName;
            TargetDeclaringType = targetDeclaringType;
            TargetName = targetName;
        }
    }
    #endregion

    /// <summary>
    /// Automatically calls <see cref="ReflectedManager.Process(Assembly)"/> for every assembly already loaded, and every assembly that is loaded in the future.
    /// </summary>
    public class ReflectedManager
    {
        private static readonly string[] _namespaceBlacklist = new[] {
            "System", "VRage", "Sandbox", "SpaceEngineers"
        };

        /// <summary>
        /// Registers the assembly load event and loads every already existing assembly.
        /// </summary>
        public void Attach()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                Process(asm);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        /// <summary>
        /// Deregisters the assembly load event
        /// </summary>
        public void Detach()
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
                foreach (FieldInfo field in t.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
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
            foreach (ReflectedMemberAttribute attr in field.GetCustomAttributes<ReflectedMemberAttribute>())
            {
                if (!field.IsStatic)
                    throw new ArgumentException("Field must be static to be reflected");
                switch (attr)
                {
                    case ReflectedMethodAttribute rma:
                        ProcessReflectedMethod(field, rma);
                        return true;
                    case ReflectedGetterAttribute rga:
                        ProcessReflectedField(field, rga);
                        return true;
                    case ReflectedSetterAttribute rsa:
                        ProcessReflectedField(field, rsa);
                        return true;
                    case ReflectedFieldInfoAttribute rfia:
                        ProcessReflectedMemberInfo(field, rfia);
                        return true;
                    case ReflectedPropertyInfoAttribute rpia:
                        ProcessReflectedMemberInfo(field, rpia);
                        return true;
                    case ReflectedMethodInfoAttribute rmia:
                        ProcessReflectedMemberInfo(field, rmia);
                        return true;
                }
            }

            var reflectedEventReplacer = field.GetCustomAttribute<ReflectedEventReplaceAttribute>();
            if (reflectedEventReplacer != null)
            {
                if (!field.IsStatic)
                    throw new ArgumentException("Field must be static to be reflected");
                field.SetValue(null, new Func<ReflectedEventReplacer>(() => new ReflectedEventReplacer(reflectedEventReplacer)));
                return true;
            }
            return false;
        }

        private static void ProcessReflectedMemberInfo(FieldInfo field, ReflectedMemberAttribute attr)
        {
            MemberInfo info = null;
            if (attr.Type == null)
                throw new ArgumentException("Reflected member info attributes require Type to be defined");
            if (attr.Name == null)
                throw new ArgumentException("Reflected member info attributes require Name to be defined");
            switch (attr)
            {
                case ReflectedFieldInfoAttribute rfia:
                    info = GetFieldPropRecursive(rfia.Type, rfia.Name,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        (a, b, c) => a.GetField(b));
                    if (info == null)
                        throw new ArgumentException($"Unable to find field {rfia.Type.FullName}#{rfia.Name}");
                    break;
                case ReflectedPropertyInfoAttribute rpia:
                    info = GetFieldPropRecursive(rpia.Type, rpia.Name,
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                            (a, b, c) => a.GetProperty(b));
                    if (info == null)
                        throw new ArgumentException($"Unable to find property {rpia.Type.FullName}#{rpia.Name}");
                    break;
                case ReflectedMethodInfoAttribute rmia:
                    if (rmia.Parameters != null)
                    {
                        info = rmia.Type.GetMethod(rmia.Name,
                             BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                             null, CallingConventions.Any, rmia.Parameters, null);
                        if (info == null)
                            throw new ArgumentException(
                                $"Unable to find method {rmia.Type.FullName}#{rmia.Name}({string.Join(", ", rmia.Parameters.Select(x => x.FullName))})");
                    }
                    else
                    {
                        info = rmia.Type.GetMethod(rmia.Name,
                             BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (info == null)
                            throw new ArgumentException(
                                $"Unable to find method {rmia.Type.FullName}#{rmia.Name}");
                    }
                    if (rmia.ReturnType != null && !rmia.ReturnType.IsAssignableFrom(((MethodInfo)info).ReturnType))
                        throw new ArgumentException($"Method {rmia.Type.FullName}#{rmia.Name} has return type {((MethodInfo)info).ReturnType.FullName}, expected {rmia.ReturnType.FullName}");
                    break;
            }
            if (info == null)
                throw new ArgumentException($"Unable to find member info for {attr.GetType().Name}[{attr.Type.FullName}#{attr.Name}");
            field.SetValue(null, info);
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

            var invokeTypes = new Type[trueParameterTypes.Length];
            for (var i = 0; i < invokeTypes.Length; i++)
                invokeTypes[i] = attr.OverrideTypes?[i] ?? trueParameterTypes[i];

            MethodInfo methodInstance = trueType.GetMethod(attr.Name ?? field.Name,
                (attr is ReflectedStaticMethodAttribute ? BindingFlags.Static : BindingFlags.Instance) |
                BindingFlags.Public |
                BindingFlags.NonPublic, null, CallingConventions.Any, invokeTypes, null);
            if (methodInstance == null)
            {
                string methodType = attr is ReflectedStaticMethodAttribute ? "static" : "instance";
                string methodParams = string.Join(", ",
                    trueParameterTypes.Select(x => x.Name));
                throw new NoNullAllowedException(
                    $"Unable to find {methodType} method {attr.Name ?? field.Name} in type {trueType.FullName} with parameters {methodParams}");
            }


            if (attr is ReflectedStaticMethodAttribute)
            {
                if (attr.OverrideTypes != null)
                {
                    ParameterExpression[] paramExp =
                        parameters.Select(x => Expression.Parameter(x.ParameterType)).ToArray();
                    var argExp = new Expression[invokeTypes.Length];
                    for (var i = 0; i < argExp.Length; i++)
                        if (invokeTypes[i] != paramExp[i].Type)
                            argExp[i] = Expression.Convert(paramExp[i], invokeTypes[i]);
                        else
                            argExp[i] = paramExp[i];
                    field.SetValue(null,
                        Expression.Lambda(Expression.Call(methodInstance, argExp), paramExp)
                                  .Compile());
                }
                else
                    field.SetValue(null, Delegate.CreateDelegate(field.FieldType, methodInstance));
            }
            else
            {
                ParameterExpression[] paramExp =
                    parameters.Select(x => Expression.Parameter(x.ParameterType)).ToArray();
                var argExp = new Expression[invokeTypes.Length];
                for (var i = 0; i < argExp.Length; i++)
                    if (invokeTypes[i] != paramExp[i + 1].Type)
                        argExp[i] = Expression.Convert(paramExp[i + 1], invokeTypes[i]);
                    else
                        argExp[i] = paramExp[i + 1];
                field.SetValue(null,
                    Expression.Lambda(Expression.Call(paramExp[0], methodInstance, argExp), paramExp)
                              .Compile());
            }
        }

        internal static T GetFieldPropRecursive<T>(Type baseType, string name, BindingFlags flags, Func<Type, string, BindingFlags, T> getter) where T : class
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

        private static void ProcessReflectedField(FieldInfo field, ReflectedMemberAttribute attr)
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

                if (!isStatic && trueType == null)
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

                if (!isStatic && trueType == null)
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
                    $"Unable to find field or property for {trueName} in {trueType.FullName} or its base types", nameof(field));

            ParameterExpression[] paramExp = parameters.Select(x => Expression.Parameter(x.ParameterType)).ToArray();
            Expression instanceExpr = null;
            if (!isStatic)
            {
                instanceExpr = trueType == paramExp[0].Type ? (Expression) paramExp[0] : Expression.Convert(paramExp[0], trueType);
            }

            MemberExpression fieldExp = sourceField != null
                                            ? Expression.Field(instanceExpr, sourceField)
                                            : Expression.Property(instanceExpr, sourceProperty);
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
