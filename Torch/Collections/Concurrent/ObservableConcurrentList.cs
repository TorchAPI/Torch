using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

#nullable enable

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Thread-safe observable list using SynchronizationContext for UI safety.
    /// Replaces MtObservableList for thread-safe observable list needs.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public sealed class ObservableConcurrentList<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly List<T?> _items = new();
        private readonly object _itemsLock = new();

        private readonly DispatcherObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler?> _collectionChangedEvent = new();
        private readonly DispatcherObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler?> _propertyChangedEvent = new();

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                _collectionChangedEvent.Add(value);
                RaisePropertyChanged(nameof(IsObserved));
            }
            remove
            {
                _collectionChangedEvent.Remove(value);
                RaisePropertyChanged(nameof(IsObserved));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                _propertyChangedEvent.Add(value);
                RaisePropertyChanged(nameof(IsObserved));
            }
            remove
            {
                _propertyChangedEvent.Remove(value);
                RaisePropertyChanged(nameof(IsObserved));
            }
        }

        /// <summary>
        /// True when there are active collection or property observers.
        /// </summary>
        public bool IsObserved => _collectionChangedEvent.IsObserved || _propertyChangedEvent.IsObserved;

        public ObservableConcurrentList()
        {
        }

        /// <summary>
        /// Only use for serialization converters!!!
        /// </summary>
        public ObservableConcurrentList(IEnumerable<T> collection) : this()
        {
            lock (_itemsLock)
            {
                foreach (var item in collection)
                    _items.Add(item); // direct add for initial population
            }
        }

        public void Add(T item)
        {
            lock (_itemsLock)
            {
                _items.Add(item);
            }
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            lock (_itemsLock)
            {
                _items.Clear();
            }
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            lock (_itemsLock)
            {
                return _items.Contains(item);
            }
        }

        public bool Remove(T item)
        {
            bool removed;
            lock (_itemsLock)
            {
                removed = _items.Remove(item);
            }
            if (removed)
            {
                InvokeCollectionChanged(new(NotifyCollectionChangedAction.Remove, item));
                RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            }
            return removed;
        }

        public void Insert(int index, T item)
        {
            lock (_itemsLock)
            {
                _items.Insert(index, item);
            }
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Moves the specified item to a new index in the list.
        /// </summary>
        /// <param name="newIndex">The index to move the item to.</param>
        /// <param name="item">The item to move.</param>
        public void Move(int newIndex, T item)
        {
            int oldIndex, adjustedNewIndex;
            lock (_itemsLock)
            {
                oldIndex = _items.IndexOf(item);
                if (oldIndex == -1)
                    throw new ArgumentException("Item not found in the list.", nameof(item));
                if (oldIndex == newIndex)
                    return;
                adjustedNewIndex = newIndex;
                // Adjust newIndex if removing before the target position
                if (oldIndex < adjustedNewIndex)
                    adjustedNewIndex--;
                _items.RemoveAt(oldIndex);
                _items.Insert(adjustedNewIndex, item);
            }
            // Raise a single Move event for UI efficiency
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Move, item, adjustedNewIndex, oldIndex));
        }

        public void RemoveAt(int index)
        {
            T? removedItem;
            lock (_itemsLock)
            {
                removedItem = _items[index];
                _items.RemoveAt(index);
            }
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        public int IndexOf(T item)
        {
            lock (_itemsLock)
            {
                return _items.IndexOf(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_itemsLock)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        public T? this[int index]
        {
            get
            {
                lock (_itemsLock)
                {
                    return _items[index];
                }
            }
            set
            {
                lock (_itemsLock)
                {
                    T? old = _items[index];
                    _items[index] = value;
                    InvokeCollectionChanged(new(NotifyCollectionChangedAction.Replace, value, old, index));
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_itemsLock)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        private void InvokeCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            _collectionChangedEvent.Raise(this, args);
            RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs args) => _propertyChangedEvent.Raise(this, args);

        private void RaisePropertyChanged(string propertyName)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_itemsLock)
            {
                return _items.ToList().GetEnumerator(); // snapshot iteration
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // --- IList explicit implementations ---
        int IList.Add(object? value)
        {
            if (value is T t)
            {
                Add(t);
                return Count - 1;
            }
            return -1;
        }

        bool IList.Contains(object? value) => value is T t && Contains(t);

        void IList.Clear() => Clear();

        int IList.IndexOf(object value) => value is T t ? IndexOf(t) : -1;

        void IList.Insert(int index, object value)
        {
            if (value is T t)
                Insert(index, t);
        }

        void IList.Remove(object value)
        {
            if (value is T t)
                Remove(t);
        }

        void IList.RemoveAt(int index) => RemoveAt(index);

        bool IList.IsReadOnly => IsReadOnly;
        bool IList.IsFixedSize => false;

        object? IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T?)value;
        }

        public void CopyTo(Array array, int index)
        {
            lock (_itemsLock)
            {
                ((ICollection)_items).CopyTo(array, index);
            }
        }

        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => _itemsLock;
    }
}
