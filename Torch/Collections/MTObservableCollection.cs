using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
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
        /// Takes a snapshot of this collection.  Note: This call is only done when a read lock is acquired.
        /// </summary>
        /// <param name="old">Collection to clear and reuse, or null if none</param>
        /// <returns>The snapshot</returns>
        protected abstract TC Snapshot(TC old);

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
            using(Lock.WriteUsing())
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
            using(Lock.WriteUsing())
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
            using(Lock.UpgradableReadUsing()) {
                int? oldIndex = (Backing as IList<TV>)?.IndexOf(item);
                if (oldIndex == -1)
                    return false;
                using(Lock.WriteUsing()) {
                    if (!Backing.Remove(item))
                        return false;
                    MarkSnapshotsDirty();

                    OnPropertyChanged(nameof(Count));
                    OnCollectionChanged(oldIndex != null
                        ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, oldIndex)
                        : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
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
        protected void OnPropertyChanged(string propName)
        {
            NotifyEvent(this, new PropertyChangedEventArgs(propName));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyEvent(this, e);
        }

        protected void NotifyEvent(object sender, PropertyChangedEventArgs args)
        {
            _propertyChangedEvent.Raise(sender, args);
        }

        protected void NotifyEvent(object sender, NotifyCollectionChangedEventArgs args)
        {
            _collectionChangedEvent.Raise(sender, args);
        }

        private readonly DispatcherEvent<PropertyChangedEventArgs, PropertyChangedEventHandler> _propertyChangedEvent =
            new DispatcherEvent<PropertyChangedEventArgs, PropertyChangedEventHandler>();
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChangedEvent.Add(value);
            remove => _propertyChangedEvent.Remove(value);
        }

        private readonly DispatcherEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler> _collectionChangedEvent =
            new DispatcherEvent<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventHandler>();
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _collectionChangedEvent.Add(value);
            remove => _collectionChangedEvent.Remove(value);
        }
        /// <summary>
        /// Event that invokes handlers registered by dispatchers on dispatchers.
        /// </summary>
        /// <typeparam name="TEvtArgs">Event argument type</typeparam>
        /// <typeparam name="TEvtHandle">Event handler delegate type</typeparam>
        private sealed class DispatcherEvent<TEvtArgs, TEvtHandle> where TEvtArgs : EventArgs
        {
            private delegate void DelInvokeHandler(TEvtHandle handler, object sender, TEvtArgs args);

            private static readonly DelInvokeHandler _invokeDirectly;
            static DispatcherEvent()
            {
                MethodInfo invoke = typeof(TEvtHandle).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                Debug.Assert(invoke != null, "No invoke method on handler type");
                _invokeDirectly = (DelInvokeHandler)Delegate.CreateDelegate(typeof(DelInvokeHandler), invoke);
            }

            private static Dispatcher CurrentDispatcher => Dispatcher.FromThread(Thread.CurrentThread);


            private event EventHandler<TEvtArgs> _event;

            internal void Raise(object sender, TEvtArgs args)
            {
                _event?.Invoke(sender, args);
            }

            internal void Add(TEvtHandle evt)
            {
                if (evt == null)
                    return;
                _event += new DispatcherDelegate(evt).Invoke;
            }

            internal void Remove(TEvtHandle evt)
            {
                if (_event == null || evt == null)
                    return;
                Delegate[] invokeList = _event.GetInvocationList();
                for (int i = invokeList.Length - 1; i >= 0; i--)
                {
                    var wrapper = (DispatcherDelegate)invokeList[i].Target;
                    Debug.Assert(wrapper._dispatcher == CurrentDispatcher, "Adding and removing should be done from the same dispatcher");
                    if (wrapper._delegate.Equals(evt))
                    {
                        _event -= wrapper.Invoke;
                        return;
                    }
                }
            }

            private struct DispatcherDelegate
            {
                internal readonly Dispatcher _dispatcher;
                internal readonly TEvtHandle _delegate;

                internal DispatcherDelegate(TEvtHandle del)
                {
                    _dispatcher = CurrentDispatcher;
                    _delegate = del;
                }

                public void Invoke(object sender, TEvtArgs args)
                {
                    if (_dispatcher == null || _dispatcher == CurrentDispatcher)
                        _invokeDirectly(_delegate, sender, args);
                    else
                        // (Delegate) (object) == dual cast so that the compiler likes it
                        _dispatcher.BeginInvoke((Delegate)(object)_delegate, DispatcherPriority.DataBind, sender, args);
                }
            }
        }

        #endregion

        #region Enumeration
        /// <summary>
        /// Manages a snapshot to a collection and dispatches enumerators from that snapshot.
        /// </summary>
        private sealed class ThreadView
        {
            private readonly MtObservableCollection<TC, TV> _owner;
            private readonly WeakReference<TC> _snapshot;
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
                _snapshot = new WeakReference<TC>(null);
                _snapshotVersion = 0;
                _snapshotRefCount = 0;
            }

            private TC GetSnapshot()
            {
                // reading the version number + snapshots
                using (_owner.Lock.ReadUsing())
                {
                    if (!_snapshot.TryGetTarget(out TC currentSnapshot) || _snapshotVersion != _owner._version)
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
