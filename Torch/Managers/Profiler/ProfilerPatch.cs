using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using Torch.Utils.Reflected;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Entity.EntityComponents.Interfaces;
using VRage.ModAPI;

namespace Torch.Managers.Profiler
{
    [PatchShim, ReflectedLazy]
    internal static class ProfilerPatch
    {
        static ProfilerPatch()
        {
            ReflectedManager.Process(typeof(ProfilerPatch));
        }

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        #region Patch Targets
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MyGameLogic), nameof(MyGameLogic.UpdateBeforeSimulation))]
        private static readonly MethodInfo _gameLogicUpdateBeforeSimulation;

        [ReflectedMethodInfo(typeof(MyGameLogic), nameof(MyGameLogic.UpdateAfterSimulation))]
        private static readonly MethodInfo _gameLogicUpdateAfterSimulation;

        [ReflectedMethodInfo(typeof(MyEntities), nameof(MyEntities.UpdateBeforeSimulation))]
        private static readonly MethodInfo _entitiesUpdateBeforeSimulation;

        [ReflectedMethodInfo(typeof(MyEntities), nameof(MyEntities.UpdateAfterSimulation))]
        private static readonly MethodInfo _entitiesUpdateAfterSimulation;
        
        [ReflectedMethodInfo(typeof(Sandbox.Engine.Platform.Game), nameof(Sandbox.Engine.Platform.Game.RunSingleFrame))]
        private static readonly MethodInfo _gameRunSingleFrame;

        [ReflectedMethodInfo(typeof(MySession), nameof(MySession.UpdateComponents))]
        private static readonly MethodInfo _sessionUpdateComponents;


        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateBeforeSimulation))]
        private static readonly MethodInfo _cubeGridSystemsUpdateBeforeSimulation;

        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateBeforeSimulation10))]
        private static readonly MethodInfo _cubeGridSystemsUpdateBeforeSimulation10;

        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateBeforeSimulation100))]
        private static readonly MethodInfo _cubeGridSystemsUpdateBeforeSimulation100;

        //        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateAfterSimulation))]
        //        private static readonly MethodInfo _cubeGridSystemsUpdateAfterSimulation;
        //
        //        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateAfterSimulation10))]
        //        private static readonly MethodInfo _cubeGridSystemsUpdateAfterSimulation10;

        [ReflectedMethodInfo(typeof(MyCubeGridSystems), nameof(MyCubeGridSystems.UpdateAfterSimulation100))]
        private static readonly MethodInfo _cubeGridSystemsUpdateAfterSimulation100;

        [ReflectedFieldInfo(typeof(MyCubeGridSystems), "m_cubeGrid")]
        private static readonly FieldInfo _gridSystemsCubeGrid;
#pragma warning restore 649
        #endregion

        private static MethodInfo _distributedUpdaterIterate;

        public static void Patch(PatchContext ctx)
        {
            _distributedUpdaterIterate = typeof(MyDistributedUpdater<,>).GetMethod("Iterate");
            ParameterInfo[] duiP = _distributedUpdaterIterate?.GetParameters();
            if (_distributedUpdaterIterate == null || duiP == null || duiP.Length != 1 || typeof(Action<>) != duiP[0].ParameterType.GetGenericTypeDefinition())
            {
                _log.Error(
                    $"Unable to find MyDistributedUpdater.Iterate(Delegate) method.  Profiling will not function.  (Found {_distributedUpdaterIterate}");
                return;
            }

            PatchDistributedUpdate(ctx, _gameLogicUpdateBeforeSimulation);
            PatchDistributedUpdate(ctx, _gameLogicUpdateAfterSimulation);
            PatchDistributedUpdate(ctx, _entitiesUpdateBeforeSimulation);
            PatchDistributedUpdate(ctx, _entitiesUpdateAfterSimulation);

            {
                MethodInfo patcher = typeof(ProfilerPatch).GetMethod(nameof(TranspilerForUpdate),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?
                    .MakeGenericMethod(typeof(MyCubeGridSystems));
                if (patcher == null)
                {
                    _log.Error($"Failed to make generic patching method for cube grid systems");
                }
                ctx.GetPattern(_cubeGridSystemsUpdateBeforeSimulation).Transpilers.Add(patcher);
                ctx.GetPattern(_cubeGridSystemsUpdateBeforeSimulation10).Transpilers.Add(patcher);
                ctx.GetPattern(_cubeGridSystemsUpdateBeforeSimulation100).Transpilers.Add(patcher);
                //                ctx.GetPattern(_cubeGridSystemsUpdateAfterSimulation).Transpilers.Add(patcher);
                //                ctx.GetPattern(_cubeGridSystemsUpdateAfterSimulation10).Transpilers.Add(patcher);
                ctx.GetPattern(_cubeGridSystemsUpdateAfterSimulation100).Transpilers.Add(patcher);
            }

            {
                MethodInfo patcher = typeof(ProfilerPatch).GetMethod(nameof(TranspilerForUpdate),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?
                    .MakeGenericMethod(typeof(MySessionComponentBase));
                if (patcher == null)
                {
                    _log.Error($"Failed to make generic patching method for session components");
                }

                ctx.GetPattern(_sessionUpdateComponents).Transpilers.Add(patcher);
            }

            ctx.GetPattern(_gameRunSingleFrame).Suffixes.Add(ProfilerData.DoRotateEntries);
        }

        #region Generalized Update Transpiler
        private static bool ShouldProfileMethodCall<T>(MethodBase info)
        {
            if (info.IsStatic)
                return false;
            if (typeof(T) != typeof(MyCubeGridSystems) &&
                !typeof(T).IsAssignableFrom(info.DeclaringType) &&
                (!typeof(MyGameLogicComponent).IsAssignableFrom(typeof(T)) || typeof(IMyGameLogicComponent) != info.DeclaringType))
                return false;
            if (typeof(T) == typeof(MySessionComponentBase) && info.Name.Equals("Simulate", StringComparison.OrdinalIgnoreCase))
                return true;
            return info.Name.StartsWith("UpdateBeforeSimulation", StringComparison.OrdinalIgnoreCase) ||
                   info.Name.StartsWith("UpdateAfterSimulation", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<MsilInstruction> TranspilerForUpdate<T>(IEnumerable<MsilInstruction> insn, Func<Type, MsilLocal> __localCreator, MethodBase __methodBase)
        {
            MethodInfo profilerCall = null;
            if (typeof(IMyEntity).IsAssignableFrom(typeof(T)))
                profilerCall = ProfilerData.GetEntityProfiler;
            else if (typeof(MyEntityComponentBase).IsAssignableFrom(typeof(T)))
                profilerCall = ProfilerData.GetEntityComponentProfiler;
            else if (typeof(MyCubeGridSystems) == typeof(T))
                profilerCall = ProfilerData.GetGridSystemProfiler;
            else if (typeof(MySessionComponentBase) == typeof(T))
                profilerCall = ProfilerData.GetSessionComponentProfiler;

            MsilLocal profilerEntry = profilerCall != null
                ? __localCreator(typeof(SlimProfilerEntry))
                : null;

            var usedLocals = new List<MsilLocal>();
            var tmpArgument = new Dictionary<Type, Stack<MsilLocal>>();

            var foundAny = false;
            foreach (MsilInstruction i in insn)
            {
                if (profilerCall != null && (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                    ShouldProfileMethodCall<T>((i.Operand as MsilOperandInline<MethodBase>)?.Value))
                {
                    MethodBase target = ((MsilOperandInline<MethodBase>)i.Operand).Value;
                    ParameterInfo[] pams = target.GetParameters();
                    usedLocals.Clear();
                    foreach (ParameterInfo pam in pams)
                    {
                        if (!tmpArgument.TryGetValue(pam.ParameterType, out var stack))
                           tmpArgument.Add(pam.ParameterType, stack = new Stack<MsilLocal>());
                        MsilLocal local = stack.Count > 0 ? stack.Pop() : __localCreator(pam.ParameterType);
                        usedLocals.Add(local);
                        yield return local.AsValueStore();
                    }

                    _log.Debug($"Attaching profiling to {target?.DeclaringType?.FullName}#{target?.Name} in {__methodBase.DeclaringType?.FullName}#{__methodBase.Name} targeting {typeof(T)}");
                    yield return new MsilInstruction(OpCodes.Dup); // duplicate the object the update is called on
                    if (typeof(MyCubeGridSystems) == typeof(T))
                    {
                        yield return new MsilInstruction(OpCodes.Ldarg_0);
                        yield return new MsilInstruction(OpCodes.Ldfld).InlineValue(_gridSystemsCubeGrid);
                    }

                    yield return new MsilInstruction(OpCodes.Call).InlineValue(profilerCall); // consume object the update is called on
                    yield return new MsilInstruction(OpCodes.Dup); // Duplicate profiler entry for brnull
                    yield return profilerEntry.AsValueStore(); // store the profiler entry for later

                    var skipProfilerOne = new MsilLabel();
                    yield return new MsilInstruction(OpCodes.Brfalse).InlineTarget(skipProfilerOne); // Brfalse == Brnull
                    {
                        yield return profilerEntry.AsValueLoad(); // start the profiler
                        yield return new MsilInstruction(OpCodes.Call).InlineValue(ProfilerData.ProfilerEntryStart);
                    }

                    // consumes from the first Dup
                    yield return new MsilInstruction(OpCodes.Nop).LabelWith(skipProfilerOne);
                    for (int j = usedLocals.Count - 1; j >= 0; j--)
                        yield return usedLocals[j].AsValueLoad();
                    yield return i;

                    var skipProfilerTwo = new MsilLabel();
                    yield return profilerEntry.AsValueLoad();
                    yield return new MsilInstruction(OpCodes.Brfalse).InlineTarget(skipProfilerTwo); // Brfalse == Brnull
                    {
                        yield return profilerEntry.AsValueLoad(); // stop the profiler
                        yield return new MsilInstruction(OpCodes.Call).InlineValue(ProfilerData.ProfilerEntryStop);
                    }
                    yield return new MsilInstruction(OpCodes.Nop).LabelWith(skipProfilerTwo);
                    foundAny = true;
                    continue;
                }
                yield return i;
            }
            if (!foundAny)
                _log.Warn($"Didn't find any update profiling targets for target {typeof(T)} in {__methodBase.DeclaringType?.FullName}#{__methodBase.Name}");
        }
        #endregion

        #region Distributed Update Targeting
        private static void PatchDistUpdateDel(PatchContext ctx, MethodBase method)
        {
            MethodRewritePattern pattern = ctx.GetPattern(method);
            MethodInfo patcher = typeof(ProfilerPatch).GetMethod(nameof(TranspilerForUpdate),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?
                .MakeGenericMethod(method.GetParameters()[0].ParameterType);
            if (patcher == null)
            {
                _log.Error($"Failed to make generic patching method for {method}");
            }
            pattern.Transpilers.Add(patcher);
        }

        private static bool IsDistributedIterate(MethodInfo info)
        {
            if (info == null)
                return false;
            if (!info.DeclaringType?.IsGenericType ?? true)
                return false;
            if (info.DeclaringType?.GetGenericTypeDefinition() != _distributedUpdaterIterate.DeclaringType)
                return false;
            ParameterInfo[] aps = _distributedUpdaterIterate.GetParameters();
            ParameterInfo[] ops = info.GetParameters();
            if (aps.Length != ops.Length)
                return false;
            for (var i = 0; i < aps.Length; i++)
                if (aps[i].ParameterType.GetGenericTypeDefinition() != ops[i].ParameterType.GetGenericTypeDefinition())
                    return false;
            return true;
        }

        private static void PatchDistributedUpdate(PatchContext ctx, MethodBase callerMethod)
        {
            var foundAnyIterate = false;
            List<MsilInstruction> msil = PatchUtilities.ReadInstructions(callerMethod).ToList();
            for (var i = 0; i < msil.Count; i++)
            {
                MsilInstruction insn = msil[i];
                if ((insn.OpCode == OpCodes.Callvirt || insn.OpCode == OpCodes.Call)
                    && IsDistributedIterate((insn.Operand as MsilOperandInline<MethodBase>)?.Value as MethodInfo))
                {
                    foundAnyIterate = true;
                    // Call to Iterate().  Backtrace up the instruction stack to find the statement creating the delegate.
                    var foundNewDel = false;
                    for (int j = i - 1; j >= 1; j--)
                    {
                        MsilInstruction insn2 = msil[j];
                        if (insn2.OpCode == OpCodes.Newobj)
                        {
                            Type ctorType = (insn2.Operand as MsilOperandInline<MethodBase>)?.Value?.DeclaringType;
                            if (ctorType != null && ctorType.IsGenericType &&
                                ctorType.GetGenericTypeDefinition() == typeof(Action<>))
                            {
                                foundNewDel = true;
                                // Find the instruction loading the function pointer this delegate is created with
                                MsilInstruction ldftn = msil[j - 1];
                                if (ldftn.OpCode != OpCodes.Ldftn ||
                                    !(ldftn.Operand is MsilOperandInline<MethodBase> targetMethod))
                                {
                                    _log.Error(
                                        $"Unable to find ldftn instruction for call to Iterate in {callerMethod.DeclaringType}#{callerMethod}");
                                }
                                else
                                {
                                    _log.Debug($"Patching {targetMethod.Value.DeclaringType}#{targetMethod.Value} for {callerMethod.DeclaringType}#{callerMethod}");
                                    PatchDistUpdateDel(ctx, targetMethod.Value);
                                }
                                break;
                            }
                        }
                    }
                    if (!foundNewDel)
                    {
                        _log.Error($"Unable to find new Action() call for Iterate in {callerMethod.DeclaringType}#{callerMethod}");
                    }
                }
            }
            if (!foundAnyIterate)
                _log.Error($"Unable to find any calls to {_distributedUpdaterIterate} in {callerMethod.DeclaringType}#{callerMethod}");
        }
        #endregion
    }
}
