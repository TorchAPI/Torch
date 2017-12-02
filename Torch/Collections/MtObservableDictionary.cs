using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe observable dictionary
    /// </summary>
    /// <typeparam name="TK">Key type</typeparam>
    /// <typeparam name="TV">Value type</typeparam>
    public class MtObservableDictionary<TK, TV> : MtObservableCollection<IDictionary<TK, TV>, KeyValuePair<TK, TV>>, IDictionary<TK, TV>
    {
        /// <summary>
        /// Creates an empty observable dictionary
        /// </summary>
        public MtObservableDictionary() : base(new Dictionary<TK, TV>())
        {
            ObservableKeys = new ProxyCollection<TK>(this, Backing.Keys, (x) => x.Key);
            ObservableValues = new ProxyCollection<TV>(this, Backing.Values, (x) => x.Value);
        }

        protected override List<KeyValuePair<TK, TV>> Snapshot(List<KeyValuePair<TK, TV>> old)
        {
            if (old == null)
                return new List<KeyValuePair<TK, TV>>(Backing);
            old.Clear();
            old.AddRange(Backing);
            return old;
        }

        /// <inheritdoc/>
        public bool ContainsKey(TK key)
        {
            using (Lock.ReadUsing())
                return Backing.ContainsKey(key);
        }

        /// <inheritdoc/>
        public void Add(TK key, TV value)
        {
            Add(new KeyValuePair<TK, TV>(key, value));
        }

        /// <inheritdoc/>
        public bool Remove(TK key)
        {
            return TryGetValue(key, out TV result) && Remove(new KeyValuePair<TK, TV>(key, result));
        }

        /// <inheritdoc/>
        public bool TryGetValue(TK key, out TV value)
        {
            using (Lock.ReadUsing())
                return Backing.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public TV this[TK key]
        {
            get
            {
                using (Lock.ReadUsing())
                    return Backing[key];
            }
            set
            {
                using (Lock.WriteUsing())
                {
                    var oldKv = new KeyValuePair<TK, TV>(key, Backing[key]);
                    var newKv = new KeyValuePair<TK, TV>(key, value);
                    Backing[key] = value;
                    MarkSnapshotsDirty();
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newKv, oldKv));
                }
            }
        }

        /// <inheritdoc/>
        public ICollection<TK> Keys => ObservableKeys;

        /// <inheritdoc/>
        public ICollection<TV> Values => ObservableValues;

        // TODO when we rewrite this to use a sorted dictionary.
        /// <inheritdoc cref="Keys"/>
        private ProxyCollection<TK> ObservableKeys { get; }

        /// <inheritdoc cref="Keys"/>
        private ProxyCollection<TV> ObservableValues { get; }

        /// <summary>
        /// Proxy collection capable of raising notifications when the parent collection changes.
        /// </summary>
        /// <typeparam name="TP">Entry type</typeparam>
        public class ProxyCollection<TP> : ICollection<TP>
        {
            private readonly MtObservableDictionary<TK, TV> _owner;
            private readonly ICollection<TP> _backing;
            private readonly Func<KeyValuePair<TK, TV>, TP> _selector;

            internal ProxyCollection(MtObservableDictionary<TK, TV> owner, ICollection<TP> backing, Func<KeyValuePair<TK, TV>, TP> selector)
            {
                _owner = owner;
                _backing = backing;
                _selector = selector;
            }

            /// <inheritdoc/>
            public IEnumerator<TP> GetEnumerator() => new TransformEnumerator<KeyValuePair<TK, TV>, TP>(_owner.GetEnumerator(), _selector);

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <inheritdoc/>
            public void Add(TP item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void Clear()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public bool Contains(TP item)
            {
                using (_owner.Lock.ReadUsing())
                    return _backing.Contains(item);
            }

            /// <inheritdoc/>
            public void CopyTo(TP[] array, int arrayIndex)
            {
                using (_owner.Lock.ReadUsing())
                    _backing.CopyTo(array, arrayIndex);
            }

            /// <inheritdoc/>
            public bool Remove(TP item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public int Count
            {
                get
                {
                    using (_owner.Lock.ReadUsing())
                        return _backing.Count;
                }
            }

            /// <inheritdoc/>
            public bool IsReadOnly => _backing.IsReadOnly;
        }
    }
}
