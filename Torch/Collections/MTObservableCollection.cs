using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe, observable collection
    /// </summary>
    /// <typeparam name="TC">Collection type</typeparam>
    /// <typeparam name="TV">Value type</typeparam>
    public abstract class MtObservableCollection<TC, TV> : MtObservableCollectionBase<TV> where TC : class, ICollection<TV>
    {
        protected readonly TC Backing;
        protected override ReaderWriterLockSlim Lock { get; }

        protected MtObservableCollection(TC backing)
        {
            // recursion so the events can read snapshots.
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            Backing = backing;
        }
        #region ICollection

        /// <inheritdoc/>
        public override void Add(TV item)
        {
            using (Lock.WriteUsing())
            {
                Backing.Add(item);
                MarkSnapshotsDirty();
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item,
                    Backing.Count - 1));
            }
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            using (Lock.WriteUsing())
            {
                Backing.Clear();
                MarkSnapshotsDirty();
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <inheritdoc/>
        public override bool Contains(TV item)
        {
            using (Lock.ReadUsing())
                return Backing.Contains(item);
        }

        /// <inheritdoc/>
        public override void CopyTo(TV[] array, int arrayIndex)
        {
            using (Lock.ReadUsing())
                Backing.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public override bool Remove(TV item)
        {
            using (Lock.UpgradableReadUsing())
            {
                int? oldIndex = (Backing as IList<TV>)?.IndexOf(item);
                if (oldIndex == -1)
                    return false;
                using (Lock.WriteUsing())
                {
                    if (!Backing.Remove(item))
                        return false;
                    MarkSnapshotsDirty();

                    OnPropertyChanged(nameof(Count));
                    OnCollectionChanged(oldIndex.HasValue
                        ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item,
                            oldIndex.Value)
                        : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return true;
                }
            }
        }

        /// <inheritdoc/>
        public override int Count
        {
            get
            {
                using (Lock.ReadUsing())
                    return Backing.Count;
            }
        }

        /// <inheritdoc/>
        public override bool IsReadOnly => Backing.IsReadOnly;

        /// <inheritdoc/>
        public override void CopyTo(Array array, int index)
        {
            using (Lock.ReadUsing())
            {
                foreach (TV k in Backing)
                {
                    array.SetValue(k, index);
                    index++;
                }
            }
        }
        #endregion
    }
}