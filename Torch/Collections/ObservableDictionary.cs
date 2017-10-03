using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Torch.Collections
{
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : ViewModel, IDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> _internalDict;

        public ObservableDictionary()
        {
            _internalDict = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _internalDict = new Dictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Create a <see cref="ObservableDictionary{TKey,TValue}"/> using the given dictionary by reference. The original dictionary should not be used after calling this.
        /// </summary>
        public static ObservableDictionary<TKey, TValue> ByReference(IDictionary<TKey, TValue> dictionary)
        {
            return new ObservableDictionary<TKey, TValue>
            {
                _internalDict = dictionary
            };
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _internalDict.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_internalDict).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _internalDict.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _internalDict.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var kv in _internalDict)
            {
                array[arrayIndex] = kv;
                arrayIndex++;
            }
        }

        /// <inheritdoc />
        public int Count => _internalDict.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return _internalDict.ContainsKey(key);
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            _internalDict.Add(key, value);
            var kv = new KeyValuePair<TKey, TValue>(key, value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, kv));
            OnPropertyChanged(nameof(Count));
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (!_internalDict.ContainsKey(key))
                return false;

            var kv = new KeyValuePair<TKey, TValue>(key, this[key]);
            if (!_internalDict.Remove(key))
                return false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, kv));
            OnPropertyChanged(nameof(Count));
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _internalDict.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get => _internalDict[key];
            set
            {
                var oldKv = new KeyValuePair<TKey, TValue>(key, _internalDict[key]);
                var newKv = new KeyValuePair<TKey, TValue>(key, value);
                _internalDict[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newKv, oldKv));
            }


        }

        /// <inheritdoc />
        public ICollection<TKey> Keys => _internalDict.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => _internalDict.Values;
    }
}
