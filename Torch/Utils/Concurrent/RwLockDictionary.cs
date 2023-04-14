using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Torch.Utils.Concurrent
{
    public class RwLockDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private int counter;

        public RwLockDictionary()
        {
        }

        public RwLockDictionary(int capacity) : base(capacity)
        {
        }

        public RwLockDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        public RwLockDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
        {
        }

        public RwLockDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }

        public RwLockDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer)
        {
        }

        protected RwLockDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginReading()
        {
            RwLock.AcquireForReading(ref counter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FinishReading()
        {
            RwLock.ReleaseAfterReading(ref counter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWriting()
        {
            RwLock.AcquireForWriting(ref counter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FinishWriting()
        {
            RwLock.ReleaseAfterWriting(ref counter);
        }
    }
}