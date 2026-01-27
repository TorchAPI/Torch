#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Thread-safe observable hash set using SynchronizationContext for UI safety.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set. Must be non-nullable.</typeparam>
    public class ObservableConcurrentHashSet<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged where T : notnull
    {
        private readonly HashSet<T> _items;
        private readonly object _lock = new();
        private readonly SynchronizationContext? _context;

        public ObservableConcurrentHashSet()
        {
            _items = new();
            _context = SynchronizationContext.Current;
        }

        public ObservableConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _items = new(comparer);
            _context = SynchronizationContext.Current;
        }

        /// <summary>
        /// Gets the number of elements contained in the set.
        /// </summary>
        public int Count
        {
            get { lock (_lock) return _items.Count; }
        }

        /// <summary>
        /// Returns false; the set is not read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Attempts to add the specified item to the set.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>true if the item was added; false if it already exists.</returns>
        public bool Add(T item)
        {
            bool added;
            lock (_lock)
            {
                added = _items.Add(item);
            }

            if (added)
            {
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new(NotifyCollectionChangedAction.Add, item));
            }

            return added;
        }

        /// <summary>
        /// Adds an item to the set (explicit implementation of ICollection<T>.Add).
        /// </summary>
        void ICollection<T>.Add(T item) => Add(item);

        /// <summary>
        /// Attempts to remove the specified item from the set.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>true if the item was removed; false if it does not exist.</returns>
        public bool Remove(T item)
        {
            bool removed;
            lock (_lock)
            {
                removed = _items.Remove(item);
            }

            if (removed)
            {
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, item));
            }

            return removed;
        }

        /// <summary>
        /// Removes all items from the set.
        /// </summary>
        public void Clear()
        {
            bool hadItems;
            lock (_lock)
            {
                hadItems = _items.Count > 0;
                if (hadItems)
                    _items.Clear();
            }

            if (hadItems)
            {
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Determines whether the set contains the specified item.
        /// </summary>
        public bool Contains(T item)
        {
            lock (_lock) return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the set to an array, starting at a particular index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set (snapshot).
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (_lock) snapshot = _items.ToList();
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Creates a list containing all elements in the set (snapshot).
        /// </summary>
        public List<T> ToList()
        {
            lock (_lock) return _items.ToList();
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_context != null && _context != SynchronizationContext.Current)
            {
                _context.Post(_ => CollectionChanged?.Invoke(this, e), null);
            }
            else
            {
                CollectionChanged?.Invoke(this, e);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_context != null && _context != SynchronizationContext.Current)
            {
                _context.Post(_ => PropertyChanged?.Invoke(this, new(propertyName)), null);
            }
            else
            {
                PropertyChanged?.Invoke(this, new(propertyName));
            }
        }
    }
}