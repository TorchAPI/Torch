#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe, read-only, observable dictionary that provides notifications
    /// when items are added, removed, or changed.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class ReadOnlyObservableConcurrentDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
        private readonly ObservableConcurrentDictionary<TKey, TValue> _source;

        /// <summary>
        /// Initializes a new instance of the ReadOnlyObservableConcurrentDictionary class.
        /// </summary>
        /// <param name="source">The source dictionary to wrap.</param>
        public ReadOnlyObservableConcurrentDictionary(ObservableConcurrentDictionary<TKey, TValue> source)
        {
            _source = source;
            _source.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
            _source.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public int Count => _source.Count;

        /// <inheritdoc/>
        public bool ContainsKey(TKey key) => _source.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value) => _source.Items.TryGetValue(key, out value!);

        /// <inheritdoc/>
        public TValue this[TKey key] => _source.Items[key];

        /// <inheritdoc/>
        public IEnumerable<TKey> Keys => _source.Items.Keys;

        /// <inheritdoc/>
        public IEnumerable<TValue> Values => _source.Items.Values;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _source.Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}