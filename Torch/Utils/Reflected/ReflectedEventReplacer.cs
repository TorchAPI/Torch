using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Torch.Utils
{
    /// <summary>
    /// Instance of statefully replacing and restoring the callbacks of an event.
    /// </summary>
    public class ReflectedEventReplacer
    {
        private const BindingFlags BindFlagAll = BindingFlags.Static |
                                                 BindingFlags.Instance |
                                                 BindingFlags.Public |
                                                 BindingFlags.NonPublic;

        private object _instance;
        private Func<IEnumerable<Delegate>> _backingStoreReader;
        private Action<Delegate> _callbackAdder;
        private Action<Delegate> _callbackRemover;
        private readonly ReflectedEventReplaceAttribute _attributes;
        private readonly HashSet<Delegate> _registeredCallbacks = new HashSet<Delegate>();
        private readonly MethodInfo _targetMethodInfo;

        internal ReflectedEventReplacer(ReflectedEventReplaceAttribute attr)
        {
            _attributes = attr;
            FieldInfo backingStore = GetEventBackingField(attr.EventName, attr.EventDeclaringType);
            if (backingStore == null)
                throw new ArgumentException($"Unable to find backing field for event {attr.EventDeclaringType.FullName}#{attr.EventName}");
            EventInfo evtInfo = ReflectedManager.GetFieldPropRecursive(attr.EventDeclaringType, attr.EventName, BindFlagAll, (a, b, c) => a.GetEvent(b, c));
            if (evtInfo == null)
                throw new ArgumentException($"Unable to find event info for event {attr.EventDeclaringType.FullName}#{attr.EventName}");
            _backingStoreReader = () => GetEventsInternal(_instance, backingStore);
            _callbackAdder = (x) => evtInfo.AddEventHandler(_instance, x);
            _callbackRemover = (x) => evtInfo.RemoveEventHandler(_instance, x);
            if (attr.TargetParameters == null)
            {
                _targetMethodInfo = attr.TargetDeclaringType.GetMethod(attr.TargetName, BindFlagAll);
                if (_targetMethodInfo == null)
                    throw new ArgumentException($"Unable to find method {attr.TargetDeclaringType.FullName}#{attr.TargetName} to replace");
            }
            else
            {
                _targetMethodInfo =
                    attr.TargetDeclaringType.GetMethod(attr.TargetName, BindFlagAll, null, attr.TargetParameters, null);
                if (_targetMethodInfo == null)
                    throw new ArgumentException($"Unable to find method {attr.TargetDeclaringType.FullName}#{attr.TargetName}){string.Join(", ", attr.TargetParameters.Select(x => x.FullName))}) to replace");
            }
        }

        /// <summary>
        /// Test that this replacement can be performed.
        /// </summary>
        /// <param name="instance">The instance to operate on, or null if static</param>
        /// <returns>true if possible, false if unsuccessful</returns>
        public bool Test(object instance)
        {
            _instance = instance;
            _registeredCallbacks.Clear();
            foreach (Delegate callback in _backingStoreReader.Invoke())
                if (callback.Method == _targetMethodInfo)
                    _registeredCallbacks.Add(callback);

            return _registeredCallbacks.Count > 0;
        }

        private Delegate _newCallback;

        /// <summary>
        /// Removes the target callback defined in the attribute and replaces it with the provided callback.
        /// </summary>
        /// <param name="newCallback">The new event callback</param>
        /// <param name="instance">The instance to operate on, or null if static</param>
        public void Replace(Delegate newCallback, object instance)
        {
            _instance = instance;
            if (_newCallback != null)
                throw new Exception("Reflected event replacer is in invalid state:  Replace when already replaced");
            _newCallback = newCallback;
            Test(instance);
            if (_registeredCallbacks.Count == 0)
                throw new Exception("Reflected event replacer is in invalid state:  Nothing to replace");
            foreach (Delegate callback in _registeredCallbacks)
                _callbackRemover.Invoke(callback);
            _callbackAdder.Invoke(_newCallback);
        }

        /// <summary>
        /// Checks if the callback is currently replaced
        /// </summary>
        public bool Replaced => _newCallback != null;

        /// <summary>
        /// Removes the callback added by <see cref="Replace"/> and puts the original callback back.
        /// </summary>
        /// <param name="instance">The instance to operate on, or null if static</param>
        public void Restore(object instance)
        {
            _instance = instance;
            if (_newCallback == null)
                throw new Exception("Reflected event replacer is in invalid state:  Restore when not replaced");
            _callbackRemover.Invoke(_newCallback);
            foreach (Delegate callback in _registeredCallbacks)
                _callbackAdder.Invoke(callback);
            _newCallback = null;
        }


        private static readonly string[] _backingFieldForEvent = { "{0}", "<backing_store>{0}" };

        private static FieldInfo GetEventBackingField(string eventName, Type baseType)
        {
            FieldInfo eventField = null;
            Type type = baseType;
            while (type != null && eventField == null)
            {
                for (var i = 0; i < _backingFieldForEvent.Length && eventField == null; i++)
                    eventField = type.GetField(string.Format(_backingFieldForEvent[i], eventName), BindFlagAll);
                type = type.BaseType;
            }
            return eventField;
        }

        private static IEnumerable<Delegate> GetEventsInternal(object instance, FieldInfo eventField)
        {
            if (eventField.GetValue(instance) is MulticastDelegate eventDel)
            {
                foreach (Delegate handle in eventDel.GetInvocationList())
                    yield return handle;
            }
        }
    }
}