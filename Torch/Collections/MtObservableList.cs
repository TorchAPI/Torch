using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe, observable list
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    public class MtObservableList<T> : MtObservableCollection<IList<T>, T>, IList<T>
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
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
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
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
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
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
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
                if (Backing is List<T> lst)
                    lst.Sort(new TransformComparer<T, TKey>(selector, comparer));
                else
                {
                    List<T> sortedItems = Backing.OrderBy(selector, comparer).ToList();
                    Backing.Clear();
                    foreach (T v in sortedItems)
                        Backing.Add(v);
                }
            }
        }
    }
}
