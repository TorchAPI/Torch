using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.Utils;
using VRage;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Torch.Managers
{
    public partial class NetworkManager : Manager, INetworkManager
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        private const string _myTransportLayerField = "TransportLayer";
        private const string _transportHandlersField = "m_handlers";
        private readonly HashSet<INetworkHandler> _networkHandlers = new HashSet<INetworkHandler>();
        private bool _init;

        [ReflectedGetter(Name = "m_typeTable")]
        private static Func<MyReplicationLayerBase, MyTypeTable> _typeTableGetter;
        [ReflectedMethod(Type = typeof(MyReplicationLayer), Name = "GetObjectByNetworkId")]
        private static Func<MyReplicationLayer, NetworkId, IMyNetObject> _getObjectByNetworkId;

        public NetworkManager(ITorchBase torchInstance) : base(torchInstance)
        {

        }
        
        private static bool ReflectionUnitTest(bool suppress = false)
        {
            try
            {
                var syncLayerType = typeof(MySyncLayer);
                var transportLayerField = syncLayerType.GetField(_myTransportLayerField, BindingFlags.NonPublic | BindingFlags.Instance);

                if (transportLayerField == null)
                    throw new TypeLoadException("Could not find internal type for TransportLayer");

                var transportLayerType = transportLayerField.FieldType;

                if (!Reflection.HasField(transportLayerType, _transportHandlersField))
                    throw new TypeLoadException("Could not find Handlers field");

                return true;
            }
            catch (TypeLoadException ex)
            {
                _log.Error(ex);
                if (suppress)
                    return false;
                throw;
            }
        }
        
        /// <inheritdoc/>
        public override void Attach()
        {
            if (_init)
                return;

            _init = true;

            if (!ReflectionUnitTest())
                throw new InvalidOperationException("Reflection unit test failed.");

            //don't bother with nullchecks here, it was all handled in ReflectionUnitTest
            var transportType = typeof(MySyncLayer).GetField(_myTransportLayerField, BindingFlags.NonPublic | BindingFlags.Instance).FieldType;
            var transportInstance = typeof(MySyncLayer).GetField(_myTransportLayerField, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(MyMultiplayer.Static.SyncLayer);
            var handlers = (IDictionary)transportType.GetField(_transportHandlersField, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(transportInstance);
            var handlerTypeField = handlers.GetType().GenericTypeArguments[0].GetField("messageId"); //Should be MyTransportLayer.HandlerId
            object id = null;
            foreach (var key in handlers.Keys)
            {
                if ((MyMessageId)handlerTypeField.GetValue(key) != MyMessageId.RPC)
                    continue;

                id = key;
                break;
            }
            if (id == null)
                throw new InvalidOperationException("RPC handler not found.");

            //remove Keen's network listener
            handlers.Remove(id);
            //replace it with our own
            handlers.Add(id, new Action<MyPacket>(OnEvent));

            //PrintDebug();

            _log.Debug("Initialized network intercept");
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            // TODO reverse what was done in Attach
        }

        #region Network Injection

        private static Dictionary<MethodInfo, Delegate> _delegateCache = new Dictionary<MethodInfo, Delegate>();

        private static Func<T, TA> GetDelegate<T, TA>(MethodInfo method) where TA : class
        {
            if (!_delegateCache.TryGetValue(method, out var del))
            {
                del = (Func<T, TA>)(x => Delegate.CreateDelegate(typeof(TA), x, method) as TA);
                _delegateCache[method] = del;
            }

            return (Func<T, TA>)del;
        }

        public static void RaiseEvent<T1>(T1 instance, MethodInfo method, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action>(method);

            MyMultiplayer.RaiseEvent(instance, del, target);
        }

        public static void RaiseEvent<T1, T2>(T1 instance, MethodInfo method, T2 arg1, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2>> (method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, target);
        }

        public static void RaiseEvent<T1, T2, T3>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5, T6>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5, T6>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, arg5, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5, T6, T7>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5, T6, T7>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, arg5, arg6, target);
        }

        public static void RaiseStaticEvent(MethodInfo method, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action>(method);

            MyMultiplayer.RaiseStaticEvent(del, target, position);
        }

        public static void RaiseStaticEvent<T1>(MethodInfo method, T1 arg1, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, target, position);
        }

        public static void RaiseStaticEvent<T1, T2>(MethodInfo method, T1 arg1, T2 arg2, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, target, position);
        }
        
        public static void RaiseStaticEvent<T1, T2, T3>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4, T5>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4, T5>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, arg5, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4, T5, T6>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4, T5, T6>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, arg5, arg6, target, position);
        }
        #endregion


    }
}
