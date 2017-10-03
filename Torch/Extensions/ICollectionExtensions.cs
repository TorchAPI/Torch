using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace Torch
{
    public static class ICollectionExtensions
    {
        /// <summary>
        /// Returns a read-only wrapped <see cref="ICollection{T}"/>
        /// </summary>
        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source as IReadOnlyCollection<T> ?? new ReadOnlyCollectionAdapter<T>(source);
        }

        /// <summary>
        /// Returns a read-only wrapped <see cref="IList{T}"/>
        /// </summary>
        public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(source);
        }

        /// <summary>
        /// Returns a read-only wrapped <see cref="IDictionary{TKey, TValue}"/>
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(source);
        }

        public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyObservable<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new ObservableReadOnlyDictionary<TKey, TValue>(source);
        }

        sealed class ObservableReadOnlyDictionary<TKey, TValue> : ViewModel, IReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> _dictionary;

            public ObservableReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;

                if (_dictionary is INotifyPropertyChanged p)
                    p.PropertyChanged += OnPropertyChanged;

                if (_dictionary is INotifyCollectionChanged c)
                    c.CollectionChanged += OnCollectionChanged;
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                OnCollectionChanged(e);
            }

            private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                OnPropertyChanged(e.PropertyName);
            }

            /// <inheritdoc />
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();

            /// <inheritdoc />
            public int Count => _dictionary.Count;

            /// <inheritdoc />
            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            /// <inheritdoc />
            public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

            /// <inheritdoc />
            public TValue this[TKey key] => _dictionary[key];

            /// <inheritdoc />
            public IEnumerable<TKey> Keys => _dictionary.Keys;

            /// <inheritdoc />
            public IEnumerable<TValue> Values => _dictionary.Values;
        }

        sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection<T> _source;

            public ReadOnlyCollectionAdapter(ICollection<T> source)
            {
                _source = source;
            }

            public int Count => _source.Count;
            public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}