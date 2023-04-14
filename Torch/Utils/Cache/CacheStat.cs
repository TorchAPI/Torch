using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Torch.Utils.Cache
{
    // Not thread safe, since we don't care about super exact results
    public class CacheStat
    {
        private long Lookups { get; set; }
        private long Hits { get; set; }
        private int Size { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            Lookups = 0;
            Hits = 0;
            Size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CountLookup(int size)
        {
            Size = size;
            Lookups++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CountHit()
        {
            Hits++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(CacheStat source)
        {
            Lookups += source.Lookups;
            Hits += source.Hits;
            Size += source.Size;

            source.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<CacheStat> stats)
        {
            foreach (var stat in stats)
                Add(stat);
        }

        public string Report
        {
            get
            {
                var lookups = Lookups;
                var hits = Hits;
                var size = Size;

                if (hits > lookups)
                    hits = lookups;

                Reset();

                var rate = lookups > 0 ? 100.0 * hits / lookups : 0;
                return $"HitRate = {rate:0.000}% = {hits}/{lookups}; ItemCount = {size}";
            }
        }
    }
}