using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe, observable collection
    /// </summary>
    /// <typeparam name="TC">Collection type</typeparam>
    /// <typeparam name="TV">Value type</typeparam>
    public abstract class MtObservableCollection<TC, TV> : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<TV> where TC : class, ICollection<TV>
    {
        protected readonly ReaderWriterLockSlim Lock;
        protected readonly TC Backing;
        private int _version;
        private readonly ThreadLocal<ThreadView> _threadViews;

        protected MtObservableCollection(TC backing)
        {
            Backing = backing;
            // recursion so the events can read snapshots.
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _version = 0;
            _threadViews = new ThreadLocal<ThreadView>(() => new ThreadView(this));
        }

        /// <summary>
        /// Should this observable collection actually dispatch events.
        /// </summary>
        public bool NotificationsEnabled { get; protected set; } = true;

        /// <summary>
        /// Takes a snapshot of this collection.  Note: This call is only done when a read lock is acquired.
        /// </summary>
        /// <param name="old">Collection to clear and reuse, or null if none</param>
        /// <returns>The snapshot</returns>
        protected abstract List<TV> Snapshot(List<TV> old);

        /// <summary>
        /// Marks all snapshots taken of this collection as dirty.
        /// </summary>
        protected void MarkSnapshotsDirty()
        {
            _version++;
        }

        #region ICollection
        /// <inheritdoc/>
        public void Add(TV item)
        {
            using (Lock.WriteUsing())
            {
                Backing.Add(item);
                MarkSnapshotsDirty();
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Backing.Count - 1));
            }
        }

        /// <inheritdoc/>
        public void Clear()
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
        public bool Contains(TV item)
        {
            using (Lock.ReadUsing())
                return Backing.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(TV[] array, int arrayIndex)
        {
            using (Lock.ReadUsing())
                Backing.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(TV item)
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
                        ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, oldIndex.Value)
                        : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return true;
                }
            }
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                using (Lock.ReadUsing())
                    return Backing.Count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => Backing.IsReadOnly;
        #endregion

        #region Event Wrappers
        private readonly WeakReference<DeferredUpdateToken> _deferredSnapshot = new WeakReference<DeferredUpdateToken>(null);
        private bool _deferredSnapshotTaken = false;
        /// <summary>
        /// Disposable that stops update signals and signals a full refresh when disposed.
        /// </summary>
        public IDisposable DeferredUpdate()
        {
            using (Lock.WriteUsing())
            {
                if (_deferredSnapshotTaken)
                    return new DummyToken();
                DeferredUpdateToken token;
                if (!_deferredSnapshot.TryGetTarget(out token))
                    _deferredSnapshot.SetTarget(token = new DeferredUpdateToken());
                token.SetCollection(this);
                return token;
            }
        }

        private struct DummyToken : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private class DeferredUpdateToken : IDisposable
        {
            private MtObservableCollection<TC, TV> _collection;

            internal void SetCollection(MtObservableCollection<TC, TV> c)
            {
                c._deferredSnapshotTaken = true;
                _collection = c;
                c.NotificationsEnabled = false;
            }

            public void Dispose()
            {
                using (_collection.Lock.WriteUsing())
                {
                    _collection.NotificationsEnabled = true;
                    _collection.OnPropertyChanged(nameof(Count));
                    _collection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _collection._deferredSnapshotTaken = false;
                }

            }
        }

        protected void OnPropertyChanged(string propName)
        {
            NotifyEvent(this, new PropertyChangedEventArgs(propName));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyEvent(this, e);
            OnPropertyChanged("Item[]");
        }

        protected void NotifyEvent(object sender, PropertyChangedEventArgs args)
        {
            if (NotificationsEnabled)
                _propertyChangedEvent.Raise(sender, args);
        }

        protected void NotifyEvent(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (NotificationsEnabled)
                _collectionChangedEvent.Raise(sender, args);
        }

        private readonly MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler> _propertyChangedEvent =
            new MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler>();
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChangedEvent.Add(value);
            remove => _propertyChangedEvent.Remove(value);
        }

        private readonly MtObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler> _collectionChangedEvent =
            new MtObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler>();
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _collectionChangedEvent.Add(value);
            remove => _collectionChangedEvent.Remove(value);
        }

        #endregion

        #region Enumeration
        /// <summary>
        /// Manages a snapshot to a collection and dispatches enumerators from that snapshot.
        /// </summary>
        private sealed class ThreadView
        {
            private readonly MtObservableCollection<TC, TV> _owner;
            private readonly WeakReference<List<TV>> _snapshot;
            /// <summary>
            /// The <see cref="MtObservableCollection{TC,TV}._version"/> of the <see cref="_snapshot"/>
            /// </summary>
            private int _snapshotVersion;
            /// <summary>
            /// Number of strong references to the value pointed to be <see cref="_snapshot"/>
            /// </summary>
            private int _snapshotRefCount;

            internal ThreadView(MtObservableCollection<TC, TV> owner)
            {
                _owner = owner;
                _snapshot = new WeakReference<List<TV>>(null);
                _snapshotVersion = 0;
                _snapshotRefCount = 0;
            }

            private List<TV> GetSnapshot()
            {
                // reading the version number + snapshots
                using (_owner.Lock.ReadUsing())
                {
                    if (!_snapshot.TryGetTarget(out List<TV> currentSnapshot) || _snapshotVersion != _owner._version)
                    {
                        // Update the snapshot, using the old one if it isn't referenced.
                        currentSnapshot = _owner.Snapshot(_snapshotRefCount == 0 ? currentSnapshot : null);
                        _snapshotVersion = _owner._version;
                        _snapshotRefCount = 0;
                        _snapshot.SetTarget(currentSnapshot);
                    }
                    return currentSnapshot;
                }
            }

            /// <summary>
            /// Borrows a snapshot from a <see cref="ThreadView"/> and provides an enumerator.
            /// Once <see cref="Dispose"/> is called the read lock is released.
            /// </summary>
            internal sealed class Enumerator : IEnumerator<TV>
            {
                private readonly IEnumerator<TV> _backing;
                private readonly ThreadView _owner;
                private bool _disposed;

                internal Enumerator(ThreadView owner)
                {
                    _owner = owner;
                    // Lock required since destructors run MT
                    lock (_owner)
                    {
                        _owner._snapshotRefCount++;
                        _backing = owner.GetSnapshot().GetEnumerator();
                    }
                    _disposed = false;
                }

                ~Enumerator()
                {
                    // Lock required since destructors run MT
                    if (!_disposed && _owner != null)
                        lock (_owner)
                            Dispose();
                }

                public void Dispose()
                {
                    // safe deref so finalizer can clean up
                    _backing?.Dispose();
                    _owner._snapshotRefCount--;
                    _disposed = true;
                }

                public bool MoveNext()
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(Enumerator));
                    return _backing.MoveNext();
                }

                public void Reset()
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(Enumerator));
                    _backing.Reset();
                }

                public TV Current
                {
                    get
                    {
                        if (_disposed)
                            throw new ObjectDisposedException(nameof(Enumerator));
                        return _backing.Current;
                    }
                }

                object IEnumerator.Current => Current;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<TV> GetEnumerator()
        {
            return new ThreadView.Enumerator(_threadViews.Value);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
