using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Torch.Collections.Concurrent
{
    /// <summary>
    /// Dispatches event handlers on the dispatcher thread they were subscribed from.
    /// </summary>
    /// <remarks>
    /// Serialization methods only capture observer counts and do not persist handlers.
    /// </remarks>
    /// <typeparam name="TEvtArgs">Event args type.</typeparam>
    /// <typeparam name="TEvtHandle">Event handler delegate type.</typeparam>
    internal sealed class DispatcherObservableEvent<TEvtArgs, TEvtHandle> where TEvtArgs : EventArgs
    {
        private delegate void InvokeHandler(TEvtHandle handler, object sender, TEvtArgs args);

        private static readonly InvokeHandler _invokeDirectly;

        static DispatcherObservableEvent()
        {
            MethodInfo invoke = typeof(TEvtHandle).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            Debug.Assert(invoke != null, "No invoke method on handler type");
            if (invoke != null) _invokeDirectly = (InvokeHandler)Delegate.CreateDelegate(typeof(InvokeHandler), invoke);
        }

        private event EventHandler<TEvtArgs> Event;

        private int _observerCount;

        public bool IsObserved => _observerCount > 0;

        public void Raise(object sender, TEvtArgs args)
        {
            Event?.Invoke(sender, args);
        }

        public void Add(TEvtHandle handler)
        {
            if (handler == null)
                return;
            Interlocked.Increment(ref _observerCount);
            Event += new DispatcherDelegate(handler).Invoke;
        }

        public void Remove(TEvtHandle handler)
        {
            if (Event == null || handler == null)
                return;

            Delegate[] invocationList = Event.GetInvocationList();
            for (int i = invocationList.Length - 1; i >= 0; i--)
            {
                var wrapper = (DispatcherDelegate)invocationList[i].Target;
                if (wrapper.Handler.Equals(handler))
                {
                    Event -= wrapper.Invoke;
                    Interlocked.Decrement(ref _observerCount);
                    return;
                }
            }
        }

        internal int ObserverCount => _observerCount;

        /// <summary>
        /// Serializes observer count to XML for diagnostics.
        /// </summary>
        internal string SerializeToXml()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DispatcherObservableEventSnapshot));
            using StringWriter sw = new StringWriter();
            serializer.Serialize(sw, new DispatcherObservableEventSnapshot { ObserverCount = _observerCount });
            return sw.ToString();
        }

        /// <summary>
        /// Deserializes observer count from XML.
        /// </summary>
        internal static DispatcherObservableEventSnapshot? DeserializeFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DispatcherObservableEventSnapshot));
                using StringReader sr = new StringReader(xml);
                return serializer.Deserialize(sr) as DispatcherObservableEventSnapshot;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"XML deserialization failed for DispatcherObservableEvent: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serializes observer count to JSON for diagnostics.
        /// </summary>
        internal string SerializeToJson()
        {
            return JsonConvert.SerializeObject(new DispatcherObservableEventSnapshot { ObserverCount = _observerCount });
        }

        /// <summary>
        /// Deserializes observer count from JSON.
        /// </summary>
        internal static DispatcherObservableEventSnapshot? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<DispatcherObservableEventSnapshot>(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"JSON deserialization failed for DispatcherObservableEvent: {ex.Message}");
                return null;
            }
        }

        private struct DispatcherDelegate
        {
            private readonly Dispatcher _dispatcher;
            internal readonly TEvtHandle Handler;

            internal DispatcherDelegate(TEvtHandle handler)
            {
                _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                Handler = handler;
            }

            public void Invoke(object sender, TEvtArgs args)
            {
                if (_dispatcher == null || _dispatcher == Dispatcher.FromThread(Thread.CurrentThread))
                    _invokeDirectly(Handler, sender, args);
                else
                    _dispatcher.BeginInvoke((Delegate)(object)Handler, DispatcherPriority.DataBind, sender, args);
            }
        }

        [XmlRoot("DispatcherObservableEvent")]
        public sealed class DispatcherObservableEventSnapshot
        {
            public int ObserverCount { get; set; }
        }
    }
}
