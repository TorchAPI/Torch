using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

#nullable enable

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Thread-safe sorted view over an observable collection, marshaled to a synchronization context.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class ObservableConcurrentSortedList<T> : IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ICollection<T> _backing;
        private readonly List<T> _store;
        private readonly object _storeLock = new();
        private IComparer<T>? _comparer;
        private Predicate<T>? _filter;

        private readonly DispatcherObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler?> _collectionChangedEvent = new();
        private readonly DispatcherObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler?> _propertyChangedEvent = new();

        public ObservableConcurrentSortedList(ICollection<T> backing, IComparer<T>? comparer)
        {
            _backing = backing ?? throw new ArgumentNullException(nameof(backing));
            _comparer = comparer;
            _store = new(_backing.Count);

            RefreshInternal();

            if (_backing is INotifyCollectionChanged notifyCollection)
                notifyCollection.CollectionChanged += BackingCollectionChanged;
            if (_backing is INotifyPropertyChanged notifyProperty)
                notifyProperty.PropertyChanged += BackingPropertyChanged;
        }

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

        public int Count => _backing.Count;

        public int FilteredCount
        {
            get
            {
                lock (_storeLock)
                {
                    return _filter == null ? _backing.Count : _store.Count;
                }
            }
        }

        public Predicate<T>? Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                Refresh();
            }
        }

        public void SetComparer(IComparer<T>? comparer, bool resort = true)
        {
            _comparer = comparer;
            if (resort)
                Sort(comparer);
        }

        public void Sort(IComparer<T>? comparer = null)
        {
            comparer ??= _comparer;
            if (comparer == null)
                return;

            lock (_storeLock)
            {
                _store.Sort(comparer);
            }

            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        public void Refresh()
        {
            lock (_storeLock)
            {
                RefreshInternal();
            }

            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(nameof(FilteredCount));
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (_storeLock)
            {
                snapshot = new(_store);
            }
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void BackingPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
            if (string.Equals(e.PropertyName, nameof(Count), StringComparison.Ordinal))
                RaisePropertyChanged(nameof(FilteredCount));
        }

        private void BackingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    HandleAdd(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    HandleRemove(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    Refresh();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleAdd(IList? items)
        {
            if (items == null || items.Count == 0)
                return;

            if (items.Count != 1)
            {
                Refresh();
                return;
            }

            var item = (T)items[0]!;
            if (_filter != null && !_filter(item))
                return;

            int index;
            lock (_storeLock)
            {
                index = InsertSorted(item);
            }

            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, item, index));
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(nameof(FilteredCount));
        }

        private void HandleRemove(IList? items)
        {
            if (items == null || items.Count == 0)
                return;

            if (items.Count != 1)
            {
                Refresh();
                return;
            }

            var item = (T)items[0]!;
            int index;
            bool removed = false;
            lock (_storeLock)
            {
                index = _store.IndexOf(item);
                if (index >= 0)
                {
                    _store.RemoveAt(index);
                    removed = true;
                }
            }

            if (!removed)
                return;

            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, item, index));
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(nameof(FilteredCount));
        }

        private void RefreshInternal()
        {
            _store.Clear();
            _store.EnsureCapacity(_backing.Count);
            foreach (var item in _backing)
            {
                if (_filter == null || _filter(item))
                    _store.Add(item);
            }

            if (_comparer != null)
                _store.Sort(_comparer);
        }

        private int InsertSorted(T item)
        {
            if (_store.Count == 0 || _comparer == null)
            {
                _store.Add(item);
                return _store.Count - 1;
            }

            if (_comparer.Compare(_store[_store.Count - 1], item) <= 0)
            {
                _store.Add(item);
                return _store.Count - 1;
            }

            if (_comparer.Compare(_store[0], item) >= 0)
            {
                _store.Insert(0, item);
                return 0;
            }

            int index = _store.BinarySearch(item, _comparer);
            if (index < 0)
                index = ~index;
            _store.Insert(index, item);
            return index;
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            _collectionChangedEvent.Raise(this, args);
        }

        private void RaisePropertyChanged(string? propertyName)
        {
            _propertyChangedEvent.Raise(this, new(propertyName));
        }
    }
}
