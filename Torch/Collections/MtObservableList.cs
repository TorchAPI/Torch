using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe, observable list
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    public class MtObservableList<T> : MtObservableCollection<List<T>, T>, IList<T>, IList
    {
        /// <summary>
        /// Initializes a new instance of the MtObservableList class that is empty and has the default initial capacity.
        /// </summary>
        public MtObservableList() : base(new List<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MtObservableList class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public MtObservableList(int capacity) : base(new List<T>(capacity))
        {
        }

        protected override List<T> Snapshot(List<T> old)
        {
            if (old == null)
            {
                var list = new List<T>(Backing);
                return list;
            }
            old.Clear();
            old.AddRange(Backing);
            return old;
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            using (Lock.ReadUsing())
                return Backing.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            using (Lock.WriteUsing())
            {
                Backing.Insert(index, item);
                MarkSnapshotsDirty();
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            using (Lock.WriteUsing())
            {
                T old = Backing[index];
                Backing.RemoveAt(index);
                MarkSnapshotsDirty();
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
            }
        }

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                using (Lock.ReadUsing())
                    return Backing[index];
            }
            set
            {
                using (Lock.ReadUsing())
                {
                    T old = Backing[index];
                    Backing[index] = value;
                    MarkSnapshotsDirty();
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                        value, old, index));
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveWhere(Func<T, bool> predicate)
        {
            for (int i = Count - 1; i >= 0; i--)
                if (predicate(this[i]))
                    RemoveAt(i);
        }

        /// <summary>
        /// Sorts the list using the given selector and comparer./>
        /// </summary>
        public void Sort<TKey>(Func<T, TKey> selector, IComparer<TKey> comparer = null)
        {
            using (DeferredUpdate())
            using (Lock.WriteUsing())
            {
                comparer = comparer ?? Comparer<TKey>.Default;
                Backing.Sort(new TransformComparer<T, TKey>(selector, comparer));
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move));
        }

        /// <summary>
        /// Sorts the list using the given comparer./>
        /// </summary>
        public void Sort(IComparer<T> comparer)
        {
            using (DeferredUpdate())
            using (Lock.WriteUsing())
            {
                Backing.Sort(comparer);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move));
        }

        /// <summary>
        /// Searches the entire list for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public int BinarySearch(T item, IComparer<T> comparer = null)
        {
            using(Lock.ReadUsing())
                return Backing.BinarySearch(item, comparer ?? Comparer<T>.Default);
        }

        /// <inheritdoc/>
        int IList.Add(object value)
        {
            if (value is T t)
                using (Lock.WriteUsing())
                {
                    int index = Backing.Count;
                    Backing.Add(t);
                    return index;
                }
            return -1;
        }

        bool IList.Contains(object value)
        {
            return value is T t && Contains(t);
        }

        int IList.IndexOf(object value)
        {
            return value is T t ? IndexOf(t) : -1;
        }

        /// <inheritdoc/>
        void IList.Insert(int index, object value)
        {
            Insert(index, (T) value);
        }

        /// <inheritdoc/>
        void IList.Remove(object value)
        {
            if (value is T t)
                base.Remove(t);
        }

        /// <inheritdoc/>
        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T) value;
        }

        /// <inheritdoc/>
        bool IList.IsFixedSize => false;
        
        /// <inheritdoc/>
        public void Move(int newIndex, object value)
        {
            if (value is T t)
            {
                base.Remove(t);
            }
            Insert(newIndex, (T)value);
        }
    }
}
