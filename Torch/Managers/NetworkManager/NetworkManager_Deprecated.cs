using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Torch.API.Managers;
using Torch.Utils;
using VRage;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Torch.Managers
{
    //Everything in this file is deprecated and should be deleted Eventually(tm)
    public partial class NetworkManager
    {
        [ReflectedGetter(Name = "m_methodInfoLookup")]
        private static Func<MyEventTable, Dictionary<MethodInfo, CallSite>> _methodInfoLookupGetter;
        private const int MAX_ARGUMENT = 6;
        private const int GENERIC_PARAMETERS = 8;
        private const int DISPATCH_PARAMETERS = 11;
        private static readonly DBNull DbNull = DBNull.Value;
        private static MethodInfo _dispatchInfo;

        private static MethodInfo DispatchEventInfo => _dispatchInfo ?? (_dispatchInfo = typeof(MyReplicationLayerBase).GetMethod("DispatchEvent", BindingFlags.NonPublic | BindingFlags.Instance));
        
        private const string _myTransportLayerField = "TransportLayer";
        private const string _transportHandlersField = "m_handlers";
        private readonly HashSet<INetworkHandler> _networkHandlers = new HashSet<INetworkHandler>();
        private bool _init;

        [ReflectedGetter(Name = "m_typeTable")]
        private static Func<MyReplicationLayerBase, MyTypeTable> _typeTableGetter;
        [ReflectedMethod(Type = typeof(MyReplicationLayer), Name = "GetObjectByNetworkId")]
        private static Func<MyReplicationLayer, NetworkId, IMyNetObject> _getObjectByNetworkId;

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
            //disable all this for now
            _log.Warn("Network intercept disabled. Some plugins may not work correctly.");
            return;

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


        /// <summary>
        /// Broadcasts an event to all connected clients
        /// </summary>
        /// <param name="method"></param>
        /// <param name="obj"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
        public void RaiseEvent(MethodInfo method, object obj, params object[] args)
        {
            //default(EndpointId) tells the network to broadcast the message
            RaiseEvent(method, obj, default(EndpointId), args);
        }

        /// <summary>
        /// Sends an event to one client by SteamId
        /// </summary>
        /// <param name="method"></param>
        /// <param name="obj"></param>
        /// <param name="steamId"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
	    public void RaiseEvent(MethodInfo method, object obj, ulong steamId, params object[] args)
        {
            RaiseEvent(method, obj, new EndpointId(steamId), args);
        }

        /// <summary>
        /// Sends an event to one client
        /// </summary>
        /// <param name="method"></param>
        /// <param name="obj"></param>
        /// <param name="endpoint"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
        public void RaiseEvent(MethodInfo method, object obj, EndpointId endpoint, params object[] args)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method), "MethodInfo cannot be null!");

            if (args.Length > MAX_ARGUMENT)
                throw new ArgumentOutOfRangeException(nameof(args), $"Cannot pass more than {MAX_ARGUMENT} arguments!");

            var owner = obj as IMyEventOwner;
            if (obj != null && owner == null)
                throw new InvalidCastException("Provided event target is not of type IMyEventOwner!");

            if (!method.HasAttribute<EventAttribute>())
                throw new CustomAttributeFormatException("Provided event target does not have the Event attribute! Replication will not succeed!");

            //array to hold arguments to pass into DispatchEvent
            object[] arguments = new object[DISPATCH_PARAMETERS];
            

            arguments[0] = obj == null ? TryGetStaticCallSite(method) : TryGetCallSite(method, obj);
            arguments[1] = endpoint;
            arguments[2] = new Vector3D?();
            arguments[3] = owner;

            //copy supplied arguments into the reflection arguments
            for (var i = 0; i < args.Length; i++)
                arguments[i + 4] = args[i];

            //pad the array out with DBNull, skip last element
            //last element should stay null (this is for blocking events -- not used?)
            for (var j = args.Length + 4; j < arguments.Length - 1; j++)
                arguments[j] = DbNull;
            
            //create an array of Types so we can create a generic method
            var argTypes = new Type[GENERIC_PARAMETERS];

            //any null arguments (not DBNull) must be of type IMyEventOwner
            for (var k = 2; k < arguments.Length; k++)
                argTypes[k - 2] = arguments[k]?.GetType() ?? typeof(IMyEventOwner);

            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (argTypes[i + 1] != parameters[i].ParameterType)
                    throw new TypeLoadException($"Type mismatch on method parameters. Expected {string.Join(", ", parameters.Select(p => p.ParameterType.ToString()))} got {string.Join(", ", argTypes.Select(t => t.ToString()))}");
            }

            //create a generic method of DispatchEvent and invoke to inject our data into the network
            var dispatch = DispatchEventInfo.MakeGenericMethod(argTypes);
            dispatch.Invoke(MyMultiplayer.ReplicationLayer, arguments);
        }

        /// <summary>
        /// Broadcasts a static event to all connected clients
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
	    public void RaiseStaticEvent(MethodInfo method, params object[] args)
        {
            //default(EndpointId) tells the network to broadcast the message
            RaiseStaticEvent(method, default(EndpointId), args);
        }

        /// <summary>
        /// Sends a static event to one client by SteamId
        /// </summary>
        /// <param name="method"></param>
        /// <param name="steamId"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
	    public void RaiseStaticEvent(MethodInfo method, ulong steamId, params object[] args)
        {
            RaiseEvent(method, (object)null, new EndpointId(steamId), args);
        }

        /// <summary>
        /// Sends a static event to one client
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <param name="args"></param>
        [Obsolete("Old injection system deprecated. Use the generic methods instead.")]
        public void RaiseStaticEvent(MethodInfo method, EndpointId endpoint, params object[] args)
        {
            RaiseEvent(method, (object)null, endpoint, args);
        }

        private CallSite TryGetStaticCallSite(MethodInfo method)
        {
            MyTypeTable typeTable = _typeTableGetter.Invoke(MyMultiplayer.ReplicationLayer);
            if (!_methodInfoLookupGetter.Invoke(typeTable.StaticEventTable).TryGetValue(method, out CallSite result))
                throw new MissingMemberException("Provided event target not found!");
            return result;
        }

        private CallSite TryGetCallSite(MethodInfo method, object arg)
        {
            MySynchronizedTypeInfo typeInfo = _typeTableGetter.Invoke(MyMultiplayer.ReplicationLayer).Get(arg.GetType());
            if (!_methodInfoLookupGetter.Invoke(typeInfo.EventTable).TryGetValue(method, out CallSite result))
                throw new MissingMemberException("Provided event target not found!");
            return result;
        }

        #endregion
    
        #region Network Intercept
        
        [Obsolete]
        //TODO: Change this to a method patch so I don't have to try to keep up with Keen.
        /// <summary>
        /// This is the main body of the network intercept system. When messages come in from clients, they are processed here
        /// before being passed on to the game server.
        /// 
        /// DO NOT modify this method unless you're absolutely sure of what you're doing. This can very easily destabilize the game!
        /// </summary>
        /// <param name="packet"></param>
        private void OnEvent(MyPacket packet)
        {
            if (_networkHandlers.Count == 0)
            {
                //pass the message back to the game server
                try
                {
                    ((MyReplicationLayer)MyMultiplayer.ReplicationLayer).OnEvent(packet);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    //crash after logging, bad things could happen if we continue on with bad data
                    throw;
                }
                return;
            }

            var stream = new BitStream();
            stream.ResetRead(packet.BitStream);

            var networkId = stream.ReadNetworkId();
            //this value is unused, but removing this line corrupts the rest of the stream
            var blockedNetworkId = stream.ReadNetworkId();
            var eventId = (uint)stream.ReadUInt16();
            bool flag = stream.ReadBool();
            Vector3D? position = new Vector3D?();
            if (flag)
                position = new Vector3D?(stream.ReadVector3D());

            CallSite site;
            object obj;
            if (networkId.IsInvalid) // Static event
            {
                site = _typeTableGetter.Invoke(MyMultiplayer.ReplicationLayer).StaticEventTable.Get(eventId);
                obj = null;
            }
            else // Instance event
            {
                //var sendAs = ((MyReplicationLayer)MyMultiplayer.ReplicationLayer).GetObjectByNetworkId(networkId);
                var sendAs = _getObjectByNetworkId((MyReplicationLayer)MyMultiplayer.ReplicationLayer, networkId);
                if (sendAs == null)
                {
                    return;
                }
                var typeInfo = _typeTableGetter.Invoke(MyMultiplayer.ReplicationLayer).Get(sendAs.GetType());
                var eventCount = typeInfo.EventTable.Count;
                if (eventId < eventCount) // Directly
                {
                    obj = sendAs;
                    site = typeInfo.EventTable.Get(eventId);
                }
                else // Through proxy
                {
                    obj = ((IMyProxyTarget)sendAs).Target;
                    typeInfo = _typeTableGetter.Invoke(MyMultiplayer.ReplicationLayer).Get(obj.GetType());
                    site = typeInfo.EventTable.Get(eventId - (uint)eventCount); // Subtract max id of Proxy
                }
            }

            //we're handling the network live in the game thread, this needs to go as fast as possible
            var discard = false;
            foreach (var handler in _networkHandlers)
            //Parallel.ForEach(_networkHandlers, handler =>
            {
                try
                {
                    if (handler.CanHandle(site))
                        discard |= handler.Handle(packet.Sender.Id.Value, site, stream, obj, packet);
                }
                catch (Exception ex)
                {
                    //ApplicationLog.Error(ex.ToString());
                    _log.Error(ex);
                }
            }

            //one of the handlers wants us to discard this packet
            if (discard)
                return;

            //pass the message back to the game server
            try
            {
                ((MyReplicationLayer)MyMultiplayer.ReplicationLayer).OnEvent(packet);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error processing network event!");
                _log.Error(ex);
                //crash after logging, bad things could happen if we continue on with bad data
                throw;
            }
        }

        [Obsolete("Deprecated. Use a method patch instead.")]
        /// <inheritdoc />
        public void RegisterNetworkHandler(INetworkHandler handler)
        {
            _log.Warn($"Plugin {handler.GetType().Assembly.FullName} registered a network handler. This system no longer works. Please alert the plugin author.");
            return;

            var handlerType = handler.GetType().FullName;
            var toRemove = new List<INetworkHandler>();
            foreach (var item in _networkHandlers)
            {
                if (item.GetType().FullName == handlerType)
                {
                    //if (ExtenderOptions.IsDebugging)
                    _log.Error("Network handler already registered! " + handlerType);
                    toRemove.Add(item);
                }
            }

            foreach (var oldHandler in toRemove)
                _networkHandlers.Remove(oldHandler);

            _networkHandlers.Add(handler);
        }

        [Obsolete("Deprecated. Use a method patch instead.")]
        /// <inheritdoc />
        public bool UnregisterNetworkHandler(INetworkHandler handler)
        {
            return _networkHandlers.Remove(handler);
        }

        [Obsolete("Deprecated. Use a method patch instead.")]
        public void RegisterNetworkHandlers(params INetworkHandler[] handlers)
        {
            foreach (var handler in handlers)
                RegisterNetworkHandler(handler);
        }

        #endregion

    }
}
