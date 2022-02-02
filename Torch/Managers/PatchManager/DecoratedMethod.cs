using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using NLog;
using Torch.Managers.PatchManager.MSIL;
using Torch.Managers.PatchManager.Transpile;
using Torch.Utils;

namespace Torch.Managers.PatchManager
{
    internal class DecoratedMethod : MethodRewritePattern
    {
        private static Action<ILHook, bool> IsAppliedSetter;

        [ReflectedMethodInfo(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), Parameters = new[] {typeof(RuntimeMethodHandle)})]
        private static MethodInfo _getMethodFromHandle = null!;
        
        [ReflectedMethodInfo(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), Parameters = new[] {typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle)})]
        private static MethodInfo _getMethodFromHandleGeneric = null!;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly MethodBase _method;

        private ILHook _hook;

        internal DecoratedMethod(MethodBase method) : base(null)
        {
            _method = method;
            if (IsAppliedSetter == null)
            {
                IsAppliedSetter = typeof(ILHook).GetProperty(nameof(ILHook.IsApplied)).CreateSetter<ILHook, bool>();
            }
        }

        internal bool HasChanged()
        {
            return Prefixes.HasChanges() || Suffixes.HasChanges() || Transpilers.HasChanges() || PostTranspilers.HasChanges();
        }

        internal void Commit()
        {
            try
            {
                // non-greedy so they are all reset
                if (!Prefixes.HasChanges(true) & !Suffixes.HasChanges(true) & !Transpilers.HasChanges(true) & !PostTranspilers.HasChanges(true))
                    return;
                Revert();

                if (Prefixes.Count == 0 && Suffixes.Count == 0 && Transpilers.Count == 0 && PostTranspilers.Count == 0)
                    return;
                _log.Log(PrintMode != 0 ? LogLevel.Info : LogLevel.Debug,
                    $"Begin patching {_method.DeclaringType?.FullName}#{_method.Name}({string.Join(", ", _method.GetParameters().Select(x => x.ParameterType.Name))})");

                if (_hook == null)
                    _hook = new ILHook(_method, Manipulator, new ILHookConfig {ManualApply = true});
                IsAppliedSetter(_hook, false);
                try
                {
                    _hook.Apply();
                }
                catch (InvalidProgramException e)
                {
                    IsAppliedSetter(_hook, false);
                    PrintMode = PrintModeEnum.Emitted | PrintModeEnum.Original;
                    try
                    {
                        _hook.Apply();
                    }
                    catch
                    {
                        // Ignore, we are already know there is an error in IL
                    }

                    throw;
                }

                _log.Log(PrintMode != 0 ? LogLevel.Info : LogLevel.Debug,
                    $"Done patching {_method.GetID()})");
            }
            catch (Exception exception)
            {
                _log.Fatal(exception, $"Error patching {_method.GetID()}");
                throw;
            }
        }

        internal void Revert()
        {
            if (_hook == null)
                return;
            _log.Debug($"Revert {_method.GetID()}");
            _hook.Dispose();
            _hook = null;
        }

        #region Create

        public const string INSTANCE_PARAMETER = "__instance";
        public const string RESULT_PARAMETER = "__result";
        public const string PREFIX_SKIPPED_PARAMETER = "__prefixSkipped";
        public const string ORIGINAL_PARAMETER = "__original";
        public const string LOCAL_PARAMETER = "__local";

        private void SavePatchedMethod(string target)
        {
            throw new NotSupportedException();
            // var asmBuilder =
            //     AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("SomeName"), AssemblyBuilderAccess.RunAndSave, Path.GetDirectoryName(target));
            // var moduleBuilder = asmBuilder.DefineDynamicModule(Path.GetFileNameWithoutExtension(target), Path.GetFileName(target));
            // var typeBuilder = moduleBuilder.DefineType("Test", TypeAttributes.Public);
            //
            //
            // var methodName = _method.Name + $"_{_patchSalt}";
            // var returnType = _method is MethodInfo meth ? meth.ReturnType : typeof(void);
            // var parameters = _method.GetParameters();
            // var parameterTypes = (_method.IsStatic ? Enumerable.Empty<Type>() : new[] {_method.DeclaringType})
            //     .Concat(parameters.Select(x => x.ParameterType)).ToArray();
            //
            // var patchMethod = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
            //     returnType, parameterTypes);
            // if (!_method.IsStatic)
            //     patchMethod.DefineParameter(0, ParameterAttributes.None, INSTANCE_PARAMETER);
            // for (var i = 0; i < parameters.Length; i++)
            //     patchMethod.DefineParameter((patchMethod.IsStatic ? 0 : 1) + i, parameters[i].Attributes, parameters[i].Name);
            //
            // var generator = new LoggingIlGenerator(patchMethod.GetILGenerator(), LogLevel.Trace);
            // List<MsilInstruction> il = EmitPatched((type, pinned) => new MsilLocal(generator.DeclareLocal(type, pinned))).ToList();
            //
            // MethodTranspiler.EmitMethod(il, generator);
            //
            // Type res = typeBuilder.CreateType();
            // asmBuilder.Save(Path.GetFileName(target));
            // foreach (var method in res.GetMethods(BindingFlags.Public | BindingFlags.Static))
            //     _log.Info($"Information " + method);
        }

        public void Manipulator(ILContext context)
        {
            context.IL.Clear();
            var generator = new LoggingIlGenerator(new CecilILGenerator(context.IL),
                PrintMode.HasFlag(PrintModeEnum.EmittedReflection) ? LogLevel.Info : LogLevel.Trace);
            List<MsilInstruction> il = EmitPatched((type, pinned) => new MsilLocal(generator.DeclareLocal(type, pinned))).ToList();

            var dumpTarget = DumpTarget != null ? File.CreateText(DumpTarget) : null;
            try
            {
                const string gap = "\n\n\n\n\n";

                void LogTarget(PrintModeEnum mode, bool err, string msg)
                {
                    if (DumpMode.HasFlag(mode))
                        dumpTarget?.WriteLine((err ? "ERROR " : "") + msg);
                    if (!PrintMode.HasFlag(mode)) return;
                    if (err)
                        _log.Error(msg);
                    else
                        _log.Info(msg);
                }

#pragma warning disable CS0612
                if (PrintMsil || DumpTarget != null)
#pragma warning restore CS0612
                {
                    lock (_log)
                    {
                        var ctx = new MethodContext(_method);
                        ctx.Read();
                        LogTarget(PrintModeEnum.Original, false, "========== Original method ==========");
                        MethodTranspiler.IntegrityAnalysis((a, b) => LogTarget(PrintModeEnum.Original, a, b), ctx.Instructions, true);
                        LogTarget(PrintModeEnum.Original, false, gap);

                        LogTarget(PrintModeEnum.Emitted, false, "========== Desired method ==========");
                        MethodTranspiler.IntegrityAnalysis((a, b) => LogTarget(PrintModeEnum.Emitted, a, b), il);
                        LogTarget(PrintModeEnum.Emitted, false, gap);
                        // If the method is invalid the program is likely to hard crash in EmitMethod or Compile, so flush the log
                        LogManager.Flush();
                    }
                }

                MethodTranspiler.EmitMethod(il, generator);

#pragma warning disable CS0612
                if (PrintMsil || DumpTarget != null)
#pragma warning restore CS0612
                {
                    lock (_log)
                    {
                        var instructions = context.Body.Instructions
                            .Select(b => new MsilInstruction(b)).ToList();
                        LogTarget(PrintModeEnum.Patched, false, "========== Patched method ==========");
                        MethodTranspiler.IntegrityAnalysis((a, b) => LogTarget(PrintModeEnum.Patched, a, b), instructions, true);
                        LogTarget(PrintModeEnum.Patched, false, gap);
                    }
                }
            }
            finally
            {
                dumpTarget?.Close();
            }
        }

        #endregion

        #region Emit

        private IEnumerable<MsilInstruction> EmitPatched(Func<Type, bool, MsilLocal> declareLocal)
        {
            var methodBody = _method.GetMethodBody();
            Debug.Assert(methodBody != null, "Method body is null");
            foreach (var localVar in methodBody.LocalVariables)
            {
                Debug.Assert(localVar.LocalType != null);
                declareLocal(localVar.LocalType, localVar.IsPinned);
            }

            var instructions = new List<MsilInstruction>();
            var specialVariables = new Dictionary<string, MsilLocal>();

            var labelAfterOriginalContent = new MsilLabel();
            var labelSkipMethodContent = new MsilLabel();


            Type returnType = _method is MethodInfo meth ? meth.ReturnType : typeof(void);
            MsilLocal resultVariable = null;
            if (returnType != typeof(void))
            {
                if (Prefixes.Concat(Suffixes).SelectMany(x => x.GetParameters()).Any(x => x.Name == RESULT_PARAMETER)
                    || Prefixes.Any(x => x.ReturnType == typeof(bool)))
                    resultVariable = declareLocal(returnType, false);
            }

            if (resultVariable != null)
                instructions.AddRange(resultVariable.SetToDefault());
            MsilLocal prefixSkippedVariable = null;
            if (Prefixes.Count > 0 && Suffixes.Any(x => x.GetParameters()
                    .Any(y => y.Name.Equals(PREFIX_SKIPPED_PARAMETER))))
            {
                prefixSkippedVariable = declareLocal(typeof(bool), false);
                specialVariables.Add(PREFIX_SKIPPED_PARAMETER, prefixSkippedVariable);
            }

            if (resultVariable != null)
                specialVariables.Add(RESULT_PARAMETER, resultVariable);

            // Create special variables
            foreach (var m in Prefixes.Concat(Suffixes))
            foreach (var param in m.GetParameters())
                if (param.Name.StartsWith(LOCAL_PARAMETER))
                {
                    var requiredType = param.ParameterType.IsByRef ? param.ParameterType.GetElementType() : param.ParameterType;
                    if (specialVariables.TryGetValue(param.Name, out var existingParam))
                    {
                        if (existingParam.Type != requiredType)
                            throw new ArgumentException(
                                $"Trying to use injected local {param.Name} for {m.DeclaringType?.FullName}#{m.ToString()} with type {requiredType} but a local with the same name already exists with type {existingParam.Type}",
                                param.Name);
                    }
                    else
                        specialVariables.Add(param.Name, declareLocal(requiredType, false));
                }

            foreach (MethodInfo prefix in Prefixes)
            {
                instructions.AddRange(EmitMonkeyCall(prefix, specialVariables));
                if (prefix.ReturnType == typeof(bool))
                    instructions.Add(new MsilInstruction(OpCodes.Brfalse).InlineTarget(labelSkipMethodContent));
                else if (prefix.ReturnType != typeof(void))
                    throw new PatchException(
                        $"Prefixes must return void or bool.  {prefix.DeclaringType?.FullName}.{prefix.Name} returns {prefix.ReturnType}", prefix);
            }

            instructions.AddRange(MethodTranspiler.Transpile(_method, (x) => declareLocal(x, false), Transpilers, labelAfterOriginalContent));

            instructions.Add(new MsilInstruction(OpCodes.Nop).LabelWith(labelAfterOriginalContent));
            if (resultVariable != null)
                instructions.Add(new MsilInstruction(OpCodes.Stloc).InlineValue(resultVariable));
            var notSkip = new MsilLabel();
            instructions.Add(new MsilInstruction(OpCodes.Br).InlineTarget(notSkip));
            instructions.Add(new MsilInstruction(OpCodes.Nop).LabelWith(labelSkipMethodContent));
            if (prefixSkippedVariable != null)
            {
                instructions.Add(new MsilInstruction(OpCodes.Ldc_I4_1));
                instructions.Add(new MsilInstruction(OpCodes.Stloc).InlineValue(prefixSkippedVariable));
            }

            instructions.Add(new MsilInstruction(OpCodes.Nop).LabelWith(notSkip));

            foreach (MethodInfo suffix in Suffixes)
            {
                instructions.AddRange(EmitMonkeyCall(suffix, specialVariables));
                if (suffix.ReturnType != typeof(void))
                    throw new PatchException($"Suffixes must return void.  {suffix.DeclaringType?.FullName}.{suffix.Name} returns {suffix.ReturnType}", suffix);
            }

            if (resultVariable != null)
                instructions.Add(new MsilInstruction(OpCodes.Ldloc).InlineValue(resultVariable));
            instructions.Add(new MsilInstruction(OpCodes.Ret));

            var result = MethodTranspiler.Transpile(_method, instructions, (x) => declareLocal(x, false), PostTranspilers, null).ToList();
            if (result.Last().OpCode != OpCodes.Ret)
                result.Add(new MsilInstruction(OpCodes.Ret));
            return result;
        }

        private IEnumerable<MsilInstruction> EmitMonkeyCall(MethodInfo patch,
            IReadOnlyDictionary<string, MsilLocal> specialVariables)
        {
            foreach (var param in patch.GetParameters())
            {
                switch (param.Name)
                {
                    case INSTANCE_PARAMETER:
                    {
                        if (_method.IsStatic)
                            throw new PatchException("Can't use an instance parameter for a static method", _method);
                        yield return new MsilInstruction(OpCodes.Ldarg_0);
                        break;
                    }
                    case ORIGINAL_PARAMETER:
                    {
                        if (!typeof(MethodBase).IsAssignableFrom(param.ParameterType))
                            throw new PatchException($"Original parameter should be assignable to {nameof(MethodBase)}",
                                _method);
                        yield return new MsilInstruction(OpCodes.Ldtoken).InlineValue(_method);
                        if (_method.DeclaringType!.ContainsGenericParameters)
                        {
                            yield return new MsilInstruction(OpCodes.Ldtoken).InlineValue(_method.DeclaringType);
                            yield return new MsilInstruction(OpCodes.Call).InlineValue(_getMethodFromHandleGeneric);
                        }
                        else
                            yield return new MsilInstruction(OpCodes.Call).InlineValue(_getMethodFromHandle);

                        if (param.ParameterType != typeof(MethodBase))
                            yield return new MsilInstruction(OpCodes.Castclass).InlineValue(param.ParameterType);
                        break;
                    }
                    case PREFIX_SKIPPED_PARAMETER:
                    {
                        if (param.ParameterType != typeof(bool))
                            throw new PatchException($"Prefix skipped parameter {param.ParameterType} must be of type bool", _method);
                        if (param.ParameterType.IsByRef || param.IsOut)
                            throw new PatchException($"Prefix skipped parameter {param.ParameterType} can't be a reference type", _method);
                        if (specialVariables.TryGetValue(PREFIX_SKIPPED_PARAMETER, out MsilLocal prefixSkip))
                            yield return new MsilInstruction(OpCodes.Ldloc).InlineValue(prefixSkip);
                        else
                            yield return new MsilInstruction(OpCodes.Ldc_I4_0);
                        break;
                    }
                    case RESULT_PARAMETER:
                    {
                        var retType = param.ParameterType.IsByRef
                            ? param.ParameterType.GetElementType()
                            : param.ParameterType;
                        if (retType == null || !retType.IsAssignableFrom(specialVariables[RESULT_PARAMETER].Type))
                            throw new PatchException(
                                $"Return type {specialVariables[RESULT_PARAMETER].Type} can't be assigned to result parameter type {retType}", _method);
                        yield return new MsilInstruction(param.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc)
                            .InlineValue(specialVariables[RESULT_PARAMETER]);
                        break;
                    }
                    default:
                    {
                        if (specialVariables.TryGetValue(param.Name, out var specialVar))
                        {
                            yield return new MsilInstruction(param.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc)
                                .InlineValue(specialVar);
                            break;
                        }

                        if (param.Name.StartsWith("__field_"))
                        {
                            var fieldName = param.Name.Substring(8);
                            var fieldDef = _method.DeclaringType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault(x => x.Name == fieldName);
                            if (fieldDef == null) throw new PatchException($"Could not find field {fieldName}", _method);
                            if (fieldDef.IsStatic)
                                yield return new MsilInstruction(param.ParameterType.IsByRef ? OpCodes.Ldsflda : OpCodes.Ldsfld)
                                    .InlineValue(fieldDef);
                            else
                            {
                                yield return new MsilInstruction(OpCodes.Ldarg_0);
                                yield return new MsilInstruction(param.ParameterType.IsByRef ? OpCodes.Ldflda : OpCodes.Ldfld)
                                    .InlineValue(fieldDef);
                            }
                            break;
                        }

                        ParameterInfo declParam = _method.GetParameters().FirstOrDefault(x => x.Name == param.Name);

                        if (declParam == null)
                            throw new PatchException($"Parameter name {param.Name} not found", _method);
                        int paramIdx = (_method.IsStatic ? 0 : 1) + declParam.Position;

                        bool patchByRef = param.IsOut || param.ParameterType.IsByRef;
                        bool declByRef = declParam.IsOut || declParam.ParameterType.IsByRef;
                        if (patchByRef == declByRef)
                            yield return new MsilInstruction(OpCodes.Ldarg).InlineValue(new MsilArgument(paramIdx));
                        else if (patchByRef)
                            yield return new MsilInstruction(OpCodes.Ldarga).InlineValue(new MsilArgument(paramIdx));
                        else
                        {
                            yield return new MsilInstruction(OpCodes.Ldarg).InlineValue(new MsilArgument(paramIdx));
                            yield return EmitExtensions.EmitDereference(declParam.ParameterType);
                        }

                        break;
                    }
                }
            }

            yield return new MsilInstruction(OpCodes.Call).InlineValue(patch);
        }

        #endregion
    }
}