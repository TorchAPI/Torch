using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using Torch.Utils;

namespace Torch.Collections
{
    /// <summary>
    /// Multithread safe, observable collection base type for event dispatch
    /// </summary>
    /// <typeparam name="TV">Value type</typeparam>
    public abstract class MtObservableCollectionBase<TV> : INotifyPropertyChanged, INotifyCollectionChanged,
        ICollection, ICollection<TV>
    {
        private int _version;
        private readonly ThreadLocal<ThreadView> _threadViews;
        protected abstract ReaderWriterLockSlim Lock { get; }

        protected MtObservableCollectionBase()
        {
            _version = 0;
            _threadViews = new ThreadLocal<ThreadView>(() => new ThreadView(this));
            _deferredSnapshot = new DeferredUpdateToken(this);
            _flushEventQueue = new Timer(FlushEventQueue);
        }

        ~MtObservableCollectionBase()
        {
            // normally we'd call Timer.Dispose() here but it's a managed handle, and the finalizer for the timerholder class does it
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

        #region Event Wrappers

        private readonly DeferredUpdateToken _deferredSnapshot;

        /// <summary>
        /// Disposable that stops update signals and signals a full refresh when disposed.
        /// </summary>
        public IDisposable DeferredUpdate()
        {
            using (Lock.WriteUsing())
            {
                _deferredSnapshot.Enter();
                return _deferredSnapshot;
            }
        }

        private class DeferredUpdateToken : IDisposable
        {
            private readonly MtObservableCollectionBase<TV> _collection;
            private int _depth;

            internal DeferredUpdateToken(MtObservableCollectionBase<TV> c)
            {
                _collection = c;
            }

            internal void Enter()
            {
                if (Interlocked.Increment(ref _depth) == 1)
                {
                    _collection.NotificationsEnabled = false;
                }
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref _depth) == 0)
                    using (_collection.Lock.WriteUsing())
                    {
                        _collection.NotificationsEnabled = true;
                        _collection.OnPropertyChanged("Count");
                        _collection.OnPropertyChanged("Item[]");
                        _collection.OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
            }
        }

        protected void OnPropertyChanged(string propName)
        {
            if (!NotificationsEnabled)
                return;
            _propertyEventQueue.Enqueue(propName);
            _flushEventQueue?.Change(_eventRaiseDelay, -1);
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!NotificationsEnabled)
                return;
            _collectionEventQueue.Enqueue(e);
            // In half a second, flush the events
            _flushEventQueue?.Change(_eventRaiseDelay, -1);
        }

        private readonly Timer _flushEventQueue;
        private const int _eventRaiseDelay = 50;

        private readonly ConcurrentQueue<NotifyCollectionChangedEventArgs> _collectionEventQueue =
            new ConcurrentQueue<NotifyCollectionChangedEventArgs>();

        private readonly ConcurrentQueue<string> _propertyEventQueue = new ConcurrentQueue<string>();

        private void FlushEventQueue(object data)
        {
            // raise property events
            while (_propertyEventQueue.TryDequeue(out string prop))
                _propertyChangedEvent.Raise(this, new PropertyChangedEventArgs(prop));

            // :/, but works better
            bool reset = _collectionEventQueue.Count > 0;
            if (reset)
                while (_collectionEventQueue.Count > 0)
                    _collectionEventQueue.TryDequeue(out _);
            else
                while (_collectionEventQueue.TryDequeue(out NotifyCollectionChangedEventArgs e))
                    _collectionChangedEvent.Raise(this, e);

            if (reset)
                _collectionChangedEvent.Raise(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private readonly MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler> _propertyChangedEvent
            =
            new MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler>();

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChangedEvent.Add(value);
                OnPropertyChanged(nameof(IsObserved));
            }
            remove
            {
                _propertyChangedEvent.Remove(value);
                OnPropertyChanged(nameof(IsObserved));
            }
        }

        private readonly MtObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler>
            _collectionChangedEvent =
                new MtObservableEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler>();
        
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                _collectionChangedEvent.Add(value);
                OnPropertyChanged(nameof(IsObserved));
            }
            remove
            {
                _collectionChangedEvent.Remove(value);
                OnPropertyChanged(nameof(IsObserved));
            }
        }

        #endregion

        /// <summary>
        /// Is this collection observed by any listeners.
        /// </summary>
        public bool IsObserved => _collectionChangedEvent.IsObserved || _propertyChangedEvent.IsObserved;

        #region Enumeration

        /// <summary>
        /// Manages a snapshot to a collection and dispatches enumerators from that snapshot.
        /// </summary>
        private sealed class ThreadView
        {
            private readonly MtObservableCollectionBase<TV> _owner;
            private readonly WeakReference<List<TV>> _snapshot;

            /// <summary>
            /// The <see cref="MtObservableCollection{TC,TV}._version"/> of the <see cref="_snapshot"/>
            /// </summary>
            private int _snapshotVersion;

            /// <summary>
            /// Number of strong references to the value pointed to be <see cref="_snapshot"/>
            /// </summary>
            private int _snapshotRefCount;

            internal ThreadView(MtObservableCollectionBase<TV> owner)
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

        /// <inheritdoc/>
        public abstract void CopyTo(Array array, int index);

        /// <inheritdoc/>
        public abstract void Add(TV item);

        /// <inheritdoc/>
        public abstract void Clear();

        /// <inheritdoc/>
        public abstract bool Contains(TV item);

        /// <inheritdoc/>
        public abstract void CopyTo(TV[] array, int arrayIndex);

        /// <inheritdoc/>
        public abstract bool Remove(TV item);

        /// <inheritdoc/>
        public abstract int Count { get; }

        /// <inheritdoc/>
        public abstract bool IsReadOnly { get; }

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => true;

        /// <inheritdoc/>
        int ICollection.Count => Count;
    }
}