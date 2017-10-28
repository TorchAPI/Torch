using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Keys = new ProxyCollection<TK>(this, Backing.Keys, (x) => x.Key);
            Values = new ProxyCollection<TV>(this, Backing.Values, (x) => x.Value);
        }

        protected override IDictionary<TK, TV> Snapshot(IDictionary<TK, TV> old)
        {
            if (old == null)
                return new Dictionary<TK, TV>(Backing);
            old.Clear();
            foreach (KeyValuePair<TK, TV> k in Backing)
                old.Add(k);
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
        public ICollection<TK> Keys { get; }

        /// <inheritdoc/>
        public ICollection<TV> Values { get; }

        internal void RaiseFullReset()
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private class ProxyCollection<TP> : ICollection<TP>
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
                using (_owner.Lock.WriteUsing())
                {
                    _backing.Add(item);
                    _owner.RaiseFullReset();
                }
            }

            /// <inheritdoc/>
            public void Clear()
            {
                _owner.Clear();
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
                using (_owner.Lock.WriteUsing())
                {
                    if (!_backing.Remove(item))
                        return false;
                    _owner.RaiseFullReset();
                    return true;
                }
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

            public bool IsReadOnly => _backing.IsReadOnly;
        }
    }
}
