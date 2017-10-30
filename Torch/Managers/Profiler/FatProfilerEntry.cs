using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Torch.Collections;

namespace Torch.Managers.Profiler
{
    public class FatProfilerEntry : SlimProfilerEntry
    {
        private readonly ConditionalWeakTable<object, SlimProfilerEntry>.CreateValueCallback
            ChildUpdateTimeCreateValueFat;
        private readonly ConditionalWeakTable<object, SlimProfilerEntry>.CreateValueCallback
            ChildUpdateTimeCreateValueSlim;
        internal readonly ConditionalWeakTable<object, SlimProfilerEntry> ChildUpdateTime = new ConditionalWeakTable<object, SlimProfilerEntry>();

        internal FatProfilerEntry() : this(null)
        {
        }

        internal FatProfilerEntry(FatProfilerEntry fat) : base(fat)
        {
            ChildUpdateTimeCreateValueFat = (key) =>
            {
                var result = new FatProfilerEntry(this);
                lock (ProfilerData.ProfilingEntriesAll)
                    ProfilerData.ProfilingEntriesAll.Add(new WeakReference<SlimProfilerEntry>(result));
                return result;
            };
            ChildUpdateTimeCreateValueSlim = (key) =>
            {
                var result = new SlimProfilerEntry(this);
                lock (ProfilerData.ProfilingEntriesAll)
                    ProfilerData.ProfilingEntriesAll.Add(new WeakReference<SlimProfilerEntry>(result));
                return result;
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SlimProfilerEntry GetSlim(object key)
        {
            return ChildUpdateTime.GetValue(key, ChildUpdateTimeCreateValueSlim);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FatProfilerEntry GetFat(object key)
        {
            return (FatProfilerEntry) ChildUpdateTime.GetValue(key, ChildUpdateTimeCreateValueFat);
        }
    }
}