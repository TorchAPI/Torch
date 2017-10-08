using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Torch.Managers.EventManager
{
    /// <summary>
    /// Represents an ordered list of callbacks.
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    public class EventList<T> : IEventList where T : IEvent
    {
        /// <summary>
        /// Delegate type for this event list
        /// </summary>
        /// <param name="evt">Event</param>
        public delegate void DelEventHandler(ref T evt);

        private struct EventHandlerData
        {
            internal readonly DelEventHandler _event;
            internal readonly EventHandlerAttribute _attribute;

            internal EventHandlerData(MethodInfo method, object instance)
            {
                _event = (DelEventHandler)Delegate.CreateDelegate(typeof(DelEventHandler), instance, method, true);
                _attribute = method.GetCustomAttribute<EventHandlerAttribute>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Raise(ref T evt)
            {
                if (!_attribute.SkipCancelled || !evt.Cancelled)
                    _event(ref evt);
            }
        }

        private bool _dispatchersDirty = false;
        private readonly List<EventHandlerData> _dispatchers = new List<EventHandlerData>();

        private int _bakedCount;
        private EventHandlerData[] _bakedDispatcher;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <inheritdoc/>
        public void AddHandler(MethodInfo method, object instance)
        {
            try
            {
                _lock.EnterWriteLock();
                _dispatchers.Add(new EventHandlerData(method, instance));
                _dispatchersDirty = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public int RemoveHandlers(object instance)
        {
            try
            {
                _lock.EnterWriteLock();
                var removeCount = 0;
                for (var i = 0; i < _dispatchers.Count; i++)
                    if (_dispatchers[i]._event.Target == instance)
                    {
                        _dispatchers.RemoveAtFast(i);
                        removeCount++;
                        i--;
                    }
                if (removeCount > 0)
                {
                    _dispatchersDirty = true;
                    _dispatchers.RemoveRange(_dispatchers.Count - removeCount, removeCount);
                }
                return removeCount;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Bake()
        {
            if (!_dispatchersDirty && _bakedDispatcher != null)
                return;
            if (_bakedDispatcher == null || _dispatchers.Count > _bakedDispatcher.Length
                || _bakedDispatcher.Length * 5 / 4 < _dispatchers.Count)
                _bakedDispatcher = new EventHandlerData[_dispatchers.Count];
            _bakedCount = _dispatchers.Count;
            for (var i = 0; i < _dispatchers.Count; i++)
                _bakedDispatcher[i] = _dispatchers[i];
            Array.Sort(_bakedDispatcher, 0, _bakedCount, EventHandlerDataComparer.Instance);
        }

        /// <summary>
        /// Raises this event for all event handlers, passing the reference to all of them
        /// </summary>
        /// <param name="evt">event to raise</param>
        public void RaiseEvent(ref T evt)
        {
            try
            {
                _lock.EnterUpgradeableReadLock();
                if (_dispatchersDirty)
                    try
                    {
                        _lock.EnterWriteLock();
                        Bake();
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                for (var i = 0; i < _bakedCount; i++)
                    _bakedDispatcher[i].Raise(ref evt);
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private class EventHandlerDataComparer : IComparer<EventHandlerData>
        {
            internal static readonly EventHandlerDataComparer Instance = new EventHandlerDataComparer();

            /// <inheritdoc cref="IComparer{EventHandlerData}.Compare"/>
            /// <remarks>
            /// This sorts event handlers with ascending priority order.
            /// </remarks>
            public int Compare(EventHandlerData x, EventHandlerData y)
            {
                return x._attribute.Priority.CompareTo(y._attribute.Priority);
            }
        }
    }
}
