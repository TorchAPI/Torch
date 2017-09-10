using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NLog;
using Torch.Managers.PatchManager.Transpile;
using Torch.Utils;

namespace Torch.Managers.PatchManager
{
    internal class DecoratedMethod : MethodRewritePattern
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly MethodBase _method;

        internal DecoratedMethod(MethodBase method) : base(null)
        {
            _method = method;
        }

        private long _revertAddress;
        private byte[] _revertData = null;
        private GCHandle? _pinnedPatch;

        internal void Commit()
        {
            if (!Prefixes.HasChanges() && !Suffixes.HasChanges() && !Transpilers.HasChanges())
                return;
            Revert();

            if (Prefixes.Count == 0 && Suffixes.Count == 0 && Transpilers.Count == 0)
                return;
            var patch = ComposePatchedMethod();

            _revertAddress = AssemblyMemory.GetMethodBodyStart(_method);
            var newAddress = AssemblyMemory.GetMethodBodyStart(patch);
            _revertData = AssemblyMemory.WriteJump(_revertAddress, newAddress);
            _pinnedPatch = GCHandle.Alloc(patch);
        }

        internal void Revert()
        {
            if (_pinnedPatch.HasValue)
            {
                AssemblyMemory.WriteMemory(_revertAddress, _revertData);
                _revertData = null;
                _pinnedPatch.Value.Free();
                _pinnedPatch = null;
            }
        }

        #region Create
        private int _patchSalt = 0;
        private DynamicMethod AllocatePatchMethod()
        {
            Debug.Assert(_method.DeclaringType != null);
            var methodName = _method.Name + $"_{_patchSalt++}";
            var returnType = _method is MethodInfo meth ? meth.ReturnType : typeof(void);
            var parameters = _method.GetParameters();
            var parameterTypes = (_method.IsStatic ? Enumerable.Empty<Type>() : new[] { typeof(object) })
                .Concat(parameters.Select(x => x.ParameterType)).ToArray();

            var patchMethod = new DynamicMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                returnType, parameterTypes, _method.DeclaringType, true);
            if (!_method.IsStatic)
                patchMethod.DefineParameter(0, ParameterAttributes.None, INSTANCE_PARAMETER);
            for (var i = 0; i < parameters.Length; i++)
                patchMethod.DefineParameter((patchMethod.IsStatic ? 0 : 1) + i, parameters[i].Attributes, parameters[i].Name);

            return patchMethod;
        }


        private const string INSTANCE_PARAMETER = "__instance";
        private const string RESULT_PARAMETER = "__result";

#pragma warning disable 649
        [ReflectedStaticMethod(Type = typeof(RuntimeHelpers), Name = "_CompileMethod", OverrideTypeNames = new[] { "System.IRuntimeMethodInfo" })]
        private static Action<object> _compileDynamicMethod;
        [ReflectedMethod(Name = "GetMethodInfo")]
        private static Func<RuntimeMethodHandle, object> _getMethodInfo;
        [ReflectedMethod(Name = "GetMethodDescriptor")]
        private static Func<DynamicMethod, RuntimeMethodHandle> _getMethodHandle;
#pragma warning restore 649

        public DynamicMethod ComposePatchedMethod()
        {
            DynamicMethod method = AllocatePatchMethod();
            var generator = new LoggingIlGenerator(method.GetILGenerator());
            EmitPatched(generator);

            // Force it to compile
            RuntimeMethodHandle handle = _getMethodHandle.Invoke(method);
            object runtimeMethodInfo = _getMethodInfo.Invoke(handle);
            _compileDynamicMethod.Invoke(runtimeMethodInfo);
            return method;
        }
        #endregion

        #region Emit
        private void EmitPatched(LoggingIlGenerator target)
        {
            var originalLocalVariables = _method.GetMethodBody().LocalVariables
                .Select(x =>
                {
                    Debug.Assert(x.LocalType != null);
                    return target.DeclareLocal(x.LocalType, x.IsPinned);
                }).ToArray();

            var specialVariables = new Dictionary<string, LocalBuilder>();

            Label? labelAfterOriginalContent = Suffixes.Count > 0 ? target.DefineLabel() : (Label?)null;
            Label? labelAfterOriginalReturn = Prefixes.Any(x => x.ReturnType == typeof(bool)) ? target.DefineLabel() : (Label?)null;


            var returnType = _method is MethodInfo meth ? meth.ReturnType : typeof(void);
            var resultVariable = returnType != typeof(void) && (labelAfterOriginalReturn.HasValue || // If we jump past main content we need local to store return val
                Prefixes.Concat(Suffixes).SelectMany(x => x.GetParameters()).Any(x => x.Name == RESULT_PARAMETER))
                ? target.DeclareLocal(returnType)
                : null;
            resultVariable?.SetToDefault(target);

            if (resultVariable != null)
                specialVariables.Add(RESULT_PARAMETER, resultVariable);

            target.EmitComment("Prefixes Begin");
            foreach (var prefix in Prefixes)
            {
                EmitMonkeyCall(target, prefix, specialVariables);
                if (prefix.ReturnType == typeof(bool))
                {
                    Debug.Assert(labelAfterOriginalReturn.HasValue);
                    target.Emit(OpCodes.Brfalse, labelAfterOriginalReturn.Value);
                }
                else if (prefix.ReturnType != typeof(void))
                    throw new Exception(
                        $"Prefixes must return void or bool.  {prefix.DeclaringType?.FullName}.{prefix.Name} returns {prefix.ReturnType}");
            }
            target.EmitComment("Prefixes End");

            target.EmitComment("Original Begin");
            MethodTranspiler.Transpile(_method, Transpilers, target, labelAfterOriginalContent);
            target.EmitComment("Original End");
            if (labelAfterOriginalContent.HasValue)
            {
                target.MarkLabel(labelAfterOriginalContent.Value);
                if (resultVariable != null)
                    target.Emit(OpCodes.Stloc, resultVariable);
            }
            if (labelAfterOriginalReturn.HasValue)
                target.MarkLabel(labelAfterOriginalReturn.Value);

            target.EmitComment("Suffixes Begin");
            foreach (var suffix in Suffixes)
            {
                EmitMonkeyCall(target, suffix, specialVariables);
                if (suffix.ReturnType != typeof(void))
                    throw new Exception($"Suffixes must return void.  {suffix.DeclaringType?.FullName}.{suffix.Name} returns {suffix.ReturnType}");
            }
            target.EmitComment("Suffixes End");

            if (labelAfterOriginalContent.HasValue || labelAfterOriginalReturn.HasValue)
            {
                if (resultVariable != null)
                    target.Emit(OpCodes.Ldloc, resultVariable);
                target.Emit(OpCodes.Ret);
            }
        }

        private void EmitMonkeyCall(LoggingIlGenerator target, MethodInfo patch,
            IReadOnlyDictionary<string, LocalBuilder> specialVariables)
        {
            target.EmitComment($"Call {patch.DeclaringType?.FullName}#{patch.Name}");
            foreach (var param in patch.GetParameters())
            {
                switch (param.Name)
                {
                    case INSTANCE_PARAMETER:
                        if (_method.IsStatic)
                            throw new Exception("Can't use an instance parameter for a static method");
                        target.Emit(OpCodes.Ldarg_0);
                        break;
                    case RESULT_PARAMETER:
                        var retType = param.ParameterType.IsByRef
                            ? param.ParameterType.GetElementType()
                            : param.ParameterType;
                        if (retType == null || !retType.IsAssignableFrom(specialVariables[RESULT_PARAMETER].LocalType))
                            throw new Exception($"Return type {specialVariables[RESULT_PARAMETER].LocalType} can't be assigned to result parameter type {retType}");
                        target.Emit(param.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc, specialVariables[RESULT_PARAMETER]);
                        break;
                    default:
                        var declParam = _method.GetParameters().FirstOrDefault(x => x.Name == param.Name);
                        if (declParam == null)
                            throw new Exception($"Parameter name {param.Name} not found");
                        var paramIdx = (_method.IsStatic ? 0 : 1) + declParam.Position;

                        var patchByRef = param.IsOut || param.ParameterType.IsByRef;
                        var declByRef = declParam.IsOut || declParam.ParameterType.IsByRef;
                        if (patchByRef == declByRef)
                            target.Emit(OpCodes.Ldarg, paramIdx);
                        else if (patchByRef)
                            target.Emit(OpCodes.Ldarga, paramIdx);
                        else
                        {
                            target.Emit(OpCodes.Ldarg, paramIdx);
                            target.EmitDereference(declParam.ParameterType);
                        }
                        break;
                }
            }
            target.Emit(OpCodes.Call, patch);
        }
        #endregion
    }
}
