using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NLog;

namespace Torch.Collections
{
    public class SortedView<T>: IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly MtObservableCollectionBase<T> _backing;
        private IComparer<T> _comparer;
        private readonly List<T> _store;

        public SortedView(MtObservableCollectionBase<T> backing, IComparer<T> comparer)
        {
            _comparer = comparer;
            _backing = backing;
            _store = new List<T>(_backing.Count);
            _store.AddRange(_backing);
            _backing.CollectionChanged += backing_CollectionChanged;
            _backing.PropertyChanged += backing_PropertyChanged;
        }

        private void backing_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void backing_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertSorted(e.NewItems);
                    CollectionChanged?.Invoke(this, e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _store.RemoveAll(r => e.OldItems.Contains(r));
                    CollectionChanged?.Invoke(this, e);
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    Refresh();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        public IEnumerator<T> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _backing.Count;

        private void InsertSorted(IEnumerable items)
        {
            foreach (var t in items)
                InsertSorted((T)t);
        }

        private int InsertSorted(T item, IComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = _comparer;

            if (_store.Count == 0 || comparer == null)
            {
                _store.Add(item);
                return 0;
            }
            if(comparer.Compare(_store[_store.Count - 1], item) <= 0)
            {
                _store.Add(item);
                return _store.Count - 1;
            }
            if(comparer.Compare(_store[0], item) >= 0)
            {
                _store.Insert(0, item);
                return 0;
            }
            int index = _store.BinarySearch(item);
            if (index < 0)
                index = ~index;
            _store.Insert(index, item);
            return index;
        }

        public void Sort(IComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = _comparer;

            if (comparer == null)
                return;

            _store.Sort(comparer);
            
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Refresh()
        {
            _store.Clear();
            //_store.AddRange(_backing);
            _store.EnsureCapacity(_backing.Count);
            foreach (var e in _backing)
                _store.Add(e);
            Sort();
        }

        public void SetComparer(IComparer<T> comparer, bool resort = true)
        {
            _comparer = comparer;
            if(resort)
                Sort();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
