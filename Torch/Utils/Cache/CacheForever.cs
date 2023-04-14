using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Torch.Utils.Concurrent;

namespace Torch.Utils.Cache
{
    public class CacheForever<TK, TV>
    {
        // First layer immutable cache, which does not need synchronization, but needs to be filled
        private IReadOnlyDictionary<TK, TV> immutableCache = new Dictionary<TK, TV>();

        // Second layer mutable and synchronized cache, collecting new keys
        private readonly RwLockDictionary<TK, TV> cache = new RwLockDictionary<TK, TV>();

#if DEBUG
        private readonly CacheStat immutableStat = new CacheStat();
        private readonly CacheStat stat = new CacheStat();
        public string ImmutableReport => immutableStat.Report;
        public string Report => stat.Report;
#endif

        public void Clear()
        {
            cache.BeginWriting();
            immutableCache = new Dictionary<TK, TV>();
            cache.Clear();
            cache.FinishWriting();
        }

        public void FillImmutableCache()
        {
            if (immutableCache.Count == cache.Count)
                return;

            cache.BeginReading();
            immutableCache = new Dictionary<TK, TV>(cache);
            cache.FinishReading();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(TK key, TV value)
        {
            cache.BeginWriting();
            cache[key] = value;
            cache.FinishWriting();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Forget(TK key)
        {
            cache.BeginWriting();
            cache.Remove(key);
            cache.FinishWriting();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TK key, out TV value)
        {
#if DEBUG
            immutableStat.CountLookup(immutableCache.Count);
#endif

            if (immutableCache.TryGetValue(key, out value))
            {
#if DEBUG
                immutableStat.CountHit();
#endif
                return true;
            }

#if DEBUG
            stat.CountLookup(cache.Count);
#endif

            cache.BeginReading();
            if (cache.TryGetValue(key, out value))
            {
                cache.FinishReading();
#if DEBUG
                stat.CountHit();
#endif
                return true;
            }

            cache.FinishReading();
            value = default;
            return false;
        }
    }
}