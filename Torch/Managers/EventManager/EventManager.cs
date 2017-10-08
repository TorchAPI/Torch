using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using VRage.Game.ModAPI;

namespace Torch.Managers.EventManager
{
    /// <summary>
    /// Manager class responsible for managing registration and dispatching of events.
    /// </summary>
    public class EventManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static Dictionary<Type, IEventList> _eventLists = new Dictionary<Type, IEventList>();

        static EventManager()
        {
            AddDispatchShim(typeof(EventShimProgrammableBlock));
        }

        private static void AddDispatchShim(Type type)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(EventList<>))
                {
                    Type eventType = field.FieldType.GenericTypeArguments[0];
                    if (_eventLists.ContainsKey(eventType))
                        _log.Error($"Ignore event dispatch list {type.FullName}#{field.Name}; we already have one.");
                    else
                        _eventLists.Add(eventType, (IEventList)field.GetValue(null));

                }
        }

        /// <summary>
        /// Finds all event handlers in the given type, and its base types
        /// </summary>
        /// <param name="exploreType">Type to explore</param>
        /// <returns>All event handlers</returns>
        private static IEnumerable<MethodInfo> EventHandlers(Type exploreType)
        {
            IEnumerable<MethodInfo> enumerable = exploreType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(x =>
                {
                    var attr = x.GetCustomAttribute<EventHandlerAttribute>();
                    if (attr == null)
                        return false;
                    ParameterInfo[] ps = x.GetParameters();
                    if (ps.Length != 1)
                        return false;
                    return ps[0].ParameterType.IsByRef && typeof(IEvent).IsAssignableFrom(ps[0].ParameterType);
                });
            return exploreType.BaseType != null ? enumerable.Concat(EventHandlers(exploreType.BaseType)) : enumerable;
        }

        /// <summary>
        /// Registers all handlers the given instance owns.
        /// </summary>
        /// <param name="instance">Instance to register handlers from</param>
        private static void RegisterHandler(object instance)
        {
            foreach (MethodInfo handler in EventHandlers(instance.GetType()))
            {
                Type eventType = handler.GetParameters()[0].ParameterType;
                if (eventType.IsInterface)
                {
                    var foundList = false;
                    foreach (KeyValuePair<Type, IEventList> kv in _eventLists)
                        if (eventType.IsAssignableFrom(kv.Key))
                        {
                            kv.Value.AddHandler(handler, instance);
                            foundList = true;
                        }
                    if (foundList)
                        continue;
                }
                else if (_eventLists.TryGetValue(eventType, out IEventList list))
                {
                    list.AddHandler(handler, instance);
                    continue;
                }
                _log.Error($"Unable to find event handler list for event type {eventType.FullName}");
            }
        }

        /// <summary>
        /// Unregisters all handlers owned by the given instance
        /// </summary>
        /// <param name="instance">Instance</param>
        private static void UnregisterHandlers(object instance)
        {
            foreach (IEventList list in _eventLists.Values)
                list.RemoveHandlers(instance);
        }

        /// <inheritdoc/>
        public EventManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }
    }
}
