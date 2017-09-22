using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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