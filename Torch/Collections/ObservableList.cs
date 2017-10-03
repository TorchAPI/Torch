using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Torch
{
    /// <summary>
    /// An observable version of <see cref="List{T}"/>.
    /// </summary>
    public class ObservableList<T> : ViewModel, IList<T>
    {
        private List<T> _internalList = new List<T>();

        /// <inheritdoc />
        public void Clear()
        {
            _internalList.Clear();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return _internalList.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            var oldIndex = _internalList.IndexOf(item);
            if (!_internalList.Remove(item))
                return false;

            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, oldIndex));
            return true;
        }

        /// <inheritdoc />
        public int Count => _internalList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(T item)
        {
            _internalList.Add(item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Count - 1));
        }

        /// <inheritdoc />
        public int IndexOf(T item) => _internalList.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            _internalList.Insert(index, item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Inserts an item in order based on the provided selector and comparer. This will only work properly on a pre-sorted list.
        /// </summary>
        public void Insert<TKey>(T item, Func<T, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            var key1 = selector(item);
            for (var i = 0; i < _internalList.Count; i++)
            {
                var key2 = selector(_internalList[i]);
                if (comparer.Compare(key1, key2) < 1)
                {
                    Insert(i, item);
                    return;
                }
            }

            Add(item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            var old = this[index];
            _internalList.RemoveAt(index);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
        }

        public T this[int index]
        {
            get => _internalList[index];
            set
            {
                var old = _internalList[index];
                if (old.Equals(value))
                    return;

                _internalList[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
            }
        }

        /// <summary>
        /// Sorts the list using the given selector and comparer./>
        /// </summary>
        public void Sort<TKey>(Func<T, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            var sortedItems = _internalList.OrderBy(selector, comparer).ToList();

            _internalList = sortedItems;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Removes all items that satisfy the given condition.
        /// </summary>
        public void RemoveWhere(Func<T, bool> condition)
        {
            for (var i = Count - 1; i > 0; i--)
            {
                if (condition?.Invoke(this[i]) ?? false)
                    RemoveAt(i);
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_internalList).GetEnumerator();
        }
    }
}
