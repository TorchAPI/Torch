using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Torch.Utils.Reflected;

namespace Torch.Utils
{
    #region MemberInfoAttributes

    #endregion

    #region FieldPropGetSet

    #endregion

    #region Invoker

    #endregion

    #region EventReplacer

    #endregion

    /// <summary>
    /// Automatically calls <see cref="ReflectedManager.Process(Assembly)"/> for every assembly already loaded, and every assembly that is loaded in the future.
    /// </summary>
    public static class ReflectedManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly HashSet<Type> _processedTypes = new HashSet<Type>();

        /// <summary>
        /// Ensures all reflected fields and methods contained in the given type are initialized
        /// </summary>
        /// <param name="t">Type to process</param>
        public static void Process(Type t)
        {
            if (_processedTypes.Add(t))
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Static | BindingFlags.Instance |
                                                        BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try
                    {
#if DEBUG
                        if (Process(field))
                            _log?.Trace(
                                $"Field {field.DeclaringType?.FullName}#{field.Name} = {field.GetValue(null) ?? "null"}");
#else
                        Process(field);
#endif
                    }
                    catch (Exception e)
                    {
                        _log?.Error(e.InnerException ?? e,
                            $"Unable to fill {field.DeclaringType?.FullName}#{field.Name}. {(e.InnerException ?? e).Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Ensures all types in the given assembly are initialized using <see cref="Process(Type)"/>
        /// </summary>
        /// <param name="asm">Assembly to process</param>
        public static void Process(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
                if (!type.HasAttribute<ReflectedLazyAttribute>())
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
                field.SetValue(null,
                    new Func<ReflectedEventReplacer>(() => new ReflectedEventReplacer(reflectedEventReplacer)));
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
                        (type, name, bindingFlags) => type.GetField(name, bindingFlags));
                    if (info == null)
                        throw new ArgumentException($"Unable to find field {rfia.Type.FullName}#{rfia.Name}");
                    break;
                case ReflectedPropertyInfoAttribute rpia:
                    info = GetFieldPropRecursive(rpia.Type, rpia.Name,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        (type, name, bindingFlags) => type.GetProperty(name, bindingFlags));
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

                    if (rmia.ReturnType != null && !rmia.ReturnType.IsAssignableFrom(((MethodInfo) info).ReturnType))
                        throw new ArgumentException(
                            $"Method {rmia.Type.FullName}#{rmia.Name} has return type {((MethodInfo) info).ReturnType.FullName}, expected {rmia.ReturnType.FullName}");
                    break;
            }

            if (info == null)
                throw new ArgumentException(
                    $"Unable to find member info for {attr.GetType().Name}[{attr.Type.FullName}#{attr.Name}");
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
                Debug.Assert(methodInstance.DeclaringType != null);
                Expression instanceExp = paramExp[0].Type != methodInstance.DeclaringType
                    ? Expression.Convert(paramExp[0], methodInstance.DeclaringType)
                    : (Expression) paramExp[0];
                field.SetValue(null,
                    Expression.Lambda(Expression.Call(instanceExp, methodInstance, argExp), paramExp)
                        .Compile());
                _log.Trace(
                    $"Reflecting field {field.DeclaringType?.FullName}#{field.Name} with {methodInstance.DeclaringType?.FullName}#{methodInstance.Name}");
            }
        }

        internal static T GetFieldPropRecursive<T>(Type baseType, string name, BindingFlags flags,
            Func<Type, string, BindingFlags, T> getter) where T : class
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
                throw new ArgumentException($"Field attribute type {attr.GetType().FullName} is invalid",
                    nameof(field));

            BindingFlags bindingFlags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Public;
            FieldInfo sourceField = GetFieldPropRecursive(trueType, trueName, bindingFlags,
                (a, b, c) => a.GetField(b, c));
            PropertyInfo sourceProperty =
                GetFieldPropRecursive(trueType, trueName, bindingFlags, (a, b, c) => a.GetProperty(b, c));
            if (sourceField == null && sourceProperty == null)
                throw new ArgumentException(
                    $"Unable to find field or property for {trueName} in {trueType.FullName} or its base types",
                    nameof(field));
            var sourceType = sourceField?.FieldType ?? sourceProperty.PropertyType;
            bool isSetter = attr is ReflectedSetterAttribute;
            if (sourceProperty != null && isSetter && !sourceProperty.CanWrite)
                throw new InvalidOperationException(
                    $"Can't create setter for readonly property {trueName} in {trueType.FullName}");

            if (sourceProperty != null && !isSetter && !sourceProperty.CanRead)
                throw new InvalidOperationException(
                    $"Can't create getter for writeonly property {trueName} in {trueType.FullName}");

            var dynMethod = new DynamicMethod((isSetter ? "set" : "get") + "_" + trueType.FullName + "." + trueName,
                delegateMethod.ReturnType, parameters.Select(x => x.ParameterType).ToArray(),
                typeof(ReflectedManager).Module, true);
            ILGenerator il = dynMethod.GetILGenerator();

            if (!isStatic)
                EmitThis(il, parameters[0].ParameterType, trueType);

            if (isSetter)
            {
                int val = isStatic ? 0 : 1;
                il.Emit(OpCodes.Ldarg, val);
                EmitCast(il, parameters[val].ParameterType, sourceType);
                if (sourceProperty != null)
                    il.Emit(sourceProperty.SetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                        sourceProperty.SetMethod);
                else
                    il.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, sourceField);
            }
            else
            {
                if (sourceProperty != null)
                    il.Emit(sourceProperty.GetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                        sourceProperty.GetMethod);
                else
                    il.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, sourceField);
                EmitCast(il, sourceType, delegateMethod.ReturnType);
            }

            il.Emit(OpCodes.Ret);

            field.SetValue(null, dynMethod.CreateDelegate(field.FieldType));
            _log.Trace(
                $"Reflecting field {field.DeclaringType?.FullName}#{field.Name} with {field.DeclaringType?.FullName}#{field.Name}");
        }

        #region IL Utils

        private static void EmitThis(ILGenerator il, Type argType, Type privateType)
        {
            il.Emit(OpCodes.Ldarg_0);
            // pointers?
            EmitCast(il, argType, privateType);
        }

        private static void EmitCast(ILGenerator il, Type from, Type to)
        {
            if (from.IsValueType && !to.IsValueType)
                il.Emit(OpCodes.Box, from);
            else if (!from.IsValueType && to.IsValueType)
            {
                il.Emit(OpCodes.Unbox, to);
                return;
            }

            if (from != to && (from.IsAssignableFrom(to) || to.IsAssignableFrom(from)))
                il.Emit(OpCodes.Castclass, to);
        }

        #endregion
    }
}