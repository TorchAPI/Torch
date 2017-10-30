using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.Utils;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Torch.Managers.Profiler
{
    /// <summary>
    /// Indicates a "fixed" profiler entry.  These always exist and will not be moved.
    /// </summary>
    internal enum ProfilerFixedEntry
    {
        Entities = 0,
        Session = 1,
        Count = 2
    }

    /// <summary>
    /// Class that stores all the timing associated with the profiler.  Use <see cref="ProfilerManager"/> for observable views into this data.
    /// </summary>
    internal class ProfilerData
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal static bool ProfileGridsUpdate = true;
        internal static bool ProfileBlocksUpdate = true;
        internal static bool ProfileEntityComponentsUpdate = true;
        internal static bool ProfileGridSystemUpdates = true;
        internal static bool ProfileSessionComponentsUpdate = true;

        private const BindingFlags _instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const BindingFlags _staticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static MethodInfo Method(Type type, string name, BindingFlags flags)
        {
            return type.GetMethod(name, flags) ?? throw new Exception($"Couldn't find method {name} on {type}");
        }

        internal static readonly MethodInfo ProfilerEntryStart = Method(typeof(SlimProfilerEntry), nameof(SlimProfilerEntry.Start), _instanceFlags);
        internal static readonly MethodInfo ProfilerEntryStop = Method(typeof(SlimProfilerEntry), nameof(SlimProfilerEntry.Stop), _instanceFlags);
        internal static readonly MethodInfo GetEntityProfiler = Method(typeof(ProfilerData), nameof(EntityEntry), _staticFlags);
        internal static readonly MethodInfo GetGridSystemProfiler = Method(typeof(ProfilerData), nameof(GridSystemEntry), _staticFlags);
        internal static readonly MethodInfo GetEntityComponentProfiler = Method(typeof(ProfilerData), nameof(EntityComponentEntry), _staticFlags);
        internal static readonly MethodInfo GetSessionComponentProfiler = Method(typeof(ProfilerData), nameof(SessionComponentEntry), _staticFlags);
        internal static readonly MethodInfo DoRotateEntries = Method(typeof(ProfilerData), nameof(RotateEntries), _staticFlags);


        internal static ProfilerEntryViewModel BindView(ProfilerEntryViewModel cache = null)
        {
            if (cache != null)
                return cache;
            lock (BoundViewModels)
                BoundViewModels.Add(new WeakReference<ProfilerEntryViewModel>(cache = new ProfilerEntryViewModel()));
            return cache;
        }

        internal static readonly List<WeakReference<ProfilerEntryViewModel>> BoundViewModels = new List<WeakReference<ProfilerEntryViewModel>>();

        #region Rotation
        public const int RotateInterval = 300;
        private static int _rotateIntervalCounter = 0;
        private static void RotateEntries()
        {
            if (_rotateIntervalCounter++ > RotateInterval)
            {
                _rotateIntervalCounter = 0;
                lock (ProfilingEntriesAll)
                {
                    var i = 0;
                    while (i < ProfilingEntriesAll.Count)
                    {
                        if (ProfilingEntriesAll[i].TryGetTarget(out SlimProfilerEntry result))
                        {
                            result.Rotate();
                            i++;
                        }
                        else
                        {
                            ProfilingEntriesAll.RemoveAtFast(i);
                        }
                    }
                }
                lock (BoundViewModels)
                {
                    var i = 0;
                    while (i < BoundViewModels.Count)
                    {
                        if (BoundViewModels[i].TryGetTarget(out ProfilerEntryViewModel model) && model.Update())
                        {
                            i++;
                        }
                        else
                        {
                            BoundViewModels.RemoveAtFast(i);
                        }
                    }
                }
            }
        }
        #endregion

        #region Internal Access
        internal static readonly List<WeakReference<SlimProfilerEntry>> ProfilingEntriesAll = new List<WeakReference<SlimProfilerEntry>>();
        internal static readonly FatProfilerEntry[] Fixed;

        internal static FatProfilerEntry FixedProfiler(ProfilerFixedEntry item)
        {
            return Fixed[(int)item] ?? throw new InvalidOperationException($"Fixed profiler {item} doesn't exist");
        }

        static ProfilerData()
        {
            Fixed = new FatProfilerEntry[(int)ProfilerFixedEntry.Count];
            lock (ProfilingEntriesAll)
                for (var i = 0; i < Fixed.Length; i++)
                {
                    Fixed[i] = new FatProfilerEntry();
                    ProfilingEntriesAll.Add(new WeakReference<SlimProfilerEntry>(Fixed[i]));
                }
        }

        // ReSharper disable ConvertToConstant.Local
        // Don't make these constants.  We need to keep the reference alive for the weak table.
        private static readonly string _gridUpdateBlocks = "Blocks";
        private static readonly string _gridUpdateSystems = "Systems";
        private static readonly string _components = "Components";
        // ReSharper restore ConvertToConstant.Local

        internal static FatProfilerEntry EntityEntry(IMyEntity entity)
        {
            if (entity == null)
                return null;
            if (entity is MyCubeBlock block)
            {
                if (!ProfileBlocksUpdate || !ProfileGridsUpdate)
                    return null;
                return EntityEntry(block.CubeGrid)?.GetFat(_gridUpdateBlocks)
                    ?.GetFat(block.BlockDefinition);
            }
            if (entity is MyCubeGrid)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (!ProfileGridsUpdate)
                    return null;
                return FixedProfiler(ProfilerFixedEntry.Entities)?.GetFat(entity);
            }
            return null;
        }

        // Arguments ordered in this BS way for ease of IL use  (dup)
        internal static SlimProfilerEntry GridSystemEntry(object system, IMyCubeGrid grid)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!ProfileGridSystemUpdates || !ProfileGridsUpdate || system == null)
                return null;
            Debug.Assert(!system.GetType().IsValueType, "Grid system was a value type.  Not good.");
            return EntityEntry(grid)?.GetFat(_gridUpdateSystems)?.GetSlim(system);
        }

        internal static SlimProfilerEntry EntityComponentEntry(MyEntityComponentBase component)
        {
            if (!ProfileEntityComponentsUpdate || component == null || component is MyCompositeGameLogicComponent)
                return null;
            return EntityEntry(component.Entity)?.GetFat(_components)?.GetSlim(component);
        }

        internal static SlimProfilerEntry SessionComponentEntry(MySessionComponentBase component)
        {
            if (!ProfileSessionComponentsUpdate || component == null)
                return null;
            return FixedProfiler(ProfilerFixedEntry.Session)?.GetSlim(component);
        }
        #endregion
    }
}
