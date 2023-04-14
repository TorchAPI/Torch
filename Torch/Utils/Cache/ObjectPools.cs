using System.Runtime.CompilerServices;
using System.Text;
using VRage.Collections;

namespace Torch.Utils.Cache
{
    public static class ObjectPools
    {
        public static readonly MyConcurrentBucketPool<StringBuilder> StringBuilder = new MyConcurrentBucketPool<StringBuilder>(typeof(ObjectPools).FullName, new StringBuilderAllocator());

        private class StringBuilderAllocator : IMyElementAllocator<StringBuilder>
        {
            private const int BucketSizeBits = 4;
            private const int BucketSize = 1 << BucketSizeBits;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public StringBuilder Allocate(int bucketId)
            {
                var capacity = bucketId << BucketSizeBits;
                return new StringBuilder(capacity, capacity + BucketSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(StringBuilder instance)
            {
                instance.Clear();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose(StringBuilder instance)
            {
                instance.Clear();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetBytes(StringBuilder instance)
            {
                return instance.Capacity;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetBucketId(StringBuilder instance)
            {
                return instance.Capacity >> BucketSizeBits;
            }

            public bool ExplicitlyDisposeAllElements => false;
        }
    }
}