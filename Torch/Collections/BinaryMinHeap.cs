using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Torch.Collections
{
    public class BinaryMinHeap<TKey, TValue> where TKey : IComparable
    {
        private struct HeapItem
        {
            public TKey Key { get; }
            public TValue Value { get; }

            public HeapItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private HeapItem[] _store;
        private readonly IComparer<TKey> _comparer;

        public int Capacity { get; private set; }
        public int Count { get; private set; }

        public bool Full => Count == Capacity;

        public BinaryMinHeap(int initialCapacity = 32, IComparer<TKey> comparer = null)
        {
            _store = new HeapItem[initialCapacity];
            Count = 0;
            Capacity = initialCapacity;
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public void Insert(TValue value, TKey key)
        {
            EnsureCapacity(Capacity + 1);

            var item = new HeapItem(key, value);

            _store[Count] = item;

            Up(Count);
            Count++;
        }

        public TValue Min()
        {
            return _store[0].Value;
        }

        public TKey MinKey()
        {
            return _store[0].Key;
        }

        public TValue RemoveMin()
        {
            TValue toReturn = _store[0].Value;

            if (Count != 1)
            {
                SwapIndices(Count - 1, 0);
                _store[Count - 1] = default(HeapItem);
                Count--;
                Down(0);
            }
            else
            {
                Count--;
                _store[0] = default(HeapItem);
            }

            return toReturn;
        }

        public TValue RemoveMax()
        {
            Debug.Assert(Count > 0);

            var maxIndex = 0;

            var maxItem = _store[0];

            for (var i = 1; i < Count; ++i)
            {
                var c = _store[i];
                if (_comparer.Compare(maxItem.Key, c.Key) < 0)
                {
                    maxIndex = i;
                    maxItem = c;
                }
            }
            
            if (maxIndex != Count)
            {
                SwapIndices(Count - 1, maxIndex);
                Up(maxIndex);
            }
            Count--;

            return maxItem.Value;
        }

        public TValue Remove(TValue value, IEqualityComparer<TValue> comparer = null)
        {
            if (Count == 0)
                return default(TValue);

            if (comparer == null)
                comparer = EqualityComparer<TValue>.Default;

            var itemIndex = -1;

            for (var i = 0; i < Count; ++i)
            {
                if (comparer.Equals(value, _store[i].Value))
                {
                    itemIndex = i;
                    break;
                }
            }

            if (itemIndex != Count && itemIndex != -1)
            {
                TValue removed = _store[itemIndex].Value;

                SwapIndices(Count - 1, itemIndex);
                Up(itemIndex);
                Down(itemIndex);

                Count--;
                return removed;
            }
            else
                return default(TValue);
        }

        public TValue Remove(TKey key)
        {
            Debug.Assert(Count > 0);

            var itemIndex = 0;

            for (var i = 1; i < Count; ++i)
            {
                if (_comparer.Compare(key, _store[i].Key) == 0)
                    itemIndex = i;
            }

            TValue removed;

            if (itemIndex != Count)
            {
                removed = _store[itemIndex].Value;

                SwapIndices(Count - 1, itemIndex);
                Up(itemIndex);
                Down(itemIndex);
            }
            else
                removed = default(TValue);

            Count--;

            return removed;
        }

        public void Clear()
        {
            Array.Clear(_store, 0, Capacity);
            Count = 0;
        }

        private void Up(int index)
        {
            if (index == 0)
                return;
            int parentIndex = (index - 1) / 2;
            HeapItem swap = _store[index];
            if (_comparer.Compare(_store[parentIndex].Key, swap.Key) <= 0)
                return;

            while (true)
            {
                SwapIndices(parentIndex, index);
                index = parentIndex;

                if (index == 0)
                    break;
                parentIndex = (index - 1) / 2;
                if (_comparer.Compare(_store[parentIndex].Key, swap.Key) <= 0)
                    break;
            }

            InsertItem(ref swap, index);
        }

        private void Down(int index)
        {
            if (Count == index + 1)
                return;

            int left = index * 2 + 1;
            int right = left + 1;

            HeapItem swap = _store[index];

            while (right <= Count) // While the current node has children
            {
                var nLeft = _store[left];
                var nRight = _store[right];

                if (right == Count || _comparer.Compare(nLeft.Key, nRight.Key) < 0) // Only the left child exists or the left child is smaller
                {
                    if (_comparer.Compare(swap.Key, nLeft.Key) <= 0)
                        break;

                    SwapIndices(left, index);

                    index = left;
                    left = index * 2 + 1;
                    right = left + 1;
                }
                else // Right child exists and is smaller
                {
                    if (_comparer.Compare(swap.Key, nRight.Key) <= 0)
                        break;

                    SwapIndices(right, index);

                    index = right;
                    left = index * 2 + 1;
                    right = left + 1;
                }
            }

            InsertItem(ref swap, index);
        }

        private void SwapIndices(int fromIndex, int toIndex)
        {
            _store[toIndex] = _store[fromIndex];
        }

        private void InsertItem(ref HeapItem fromItem, int toIndex)
        {
            _store[toIndex] = fromItem;
        }

        public void EnsureCapacity(int capacity)
        {
            if (_store.Length >= capacity)
                return;

            //double capacity until we reach the minimum requested capacity (or greater)
            int newcap = Capacity * 2;
            while (newcap < capacity)
                newcap *= 2;

            var newArray = new HeapItem[newcap];
            Array.Copy(_store, newArray, Capacity);

            _store = newArray;
            Capacity = newcap;
        }
    }
}
