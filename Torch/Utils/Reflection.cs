using System;
using System.Collections.Generic;
using System.Reflection;
using NLog;

namespace Torch.Utils
{
    public static class Reflection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static bool HasMethod(Type type, string methodName, Type[] argTypes = null)
        {
            try
            {
                if (string.IsNullOrEmpty(methodName))
                    return false;

                if (argTypes == null)
                {
                    var methodInfo = type.GetMethod(methodName);
                    if (methodInfo == null)
                        methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (methodInfo == null && type.BaseType != null)
                        methodInfo = type.BaseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (methodInfo == null)
                    {
                        Log.Error("Failed to find method '" + methodName + "' in type '" + type.FullName + "'");
                        return false;
                    }
                }
                else
                {
                    MethodInfo method = type.GetMethod(methodName, argTypes);
                    if (method == null)
                        method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
                    if (method == null && type.BaseType != null)
                        method = type.BaseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
                    if (method == null)
                    {
                        Log.Error("Failed to find method '" + methodName + "' in type '" + type.FullName + "'");
                        return false;
                    }
                }

                return true;
            }
            catch (AmbiguousMatchException)
            {
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to find method '" + methodName + "' in type '" + type.FullName + "': " + ex.Message);
                Log.Error(ex);
                return false;
            }
        }

        public static bool HasField(Type type, string fieldName)
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName))
                    return false;
                var field = type.GetField(fieldName);
                if (field == null)
                    field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field == null)
                    field = type.BaseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field == null)
                {
                    Log.Error("Failed to find field '{0}' in type '{1}'", fieldName, type.FullName);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to find field '{0}' in type '{1}'", fieldName, type.FullName);
                return false;
            }
        }

        public static bool HasProperty(Type type, string propertyName)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyName))
                    return false;
                var prop = type.GetProperty(propertyName);
                if (prop == null)
                    prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (prop == null)
                    prop = type.BaseType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (prop == null)
                {
                    Log.Error("Failed to find property '{0}' in type '{1}'", propertyName, type.FullName);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to find property '{0}' in type '{1}'", propertyName, type.FullName);
                return false;
            }
        }

        /// <summary>
        /// Invokes the static method of the given type, with the given arguments.
        /// </summary>
        /// <param name="type">Type the method is contained in</param>
        /// <param name="methodName">Method name</param>
        /// <param name="args">Arguments to the method</param>
        /// <returns>return value of the invoked method, or null if it failed</returns>
        public static object InvokeStaticMethod(Type type, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Error($"Method {methodName} not found in static class {type.FullName}");
                return null;
            }

            return method.Invoke(null, args);
        }

        /// <summary>
        /// Invokes the private method with the given arguments on the instance.  Includes base types of instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns>the return value of the method, or null if it failed</returns>
        public static object InvokePrivateMethod(object instance, string methodName, params object[] args)
        {
            Type type = instance.GetType();
            while (type != null)
            {
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method.Invoke(instance, args);
                type = type.BaseType;
            }

            Log.Error($"Method {methodName} not found in type {instance.GetType().FullName} or its parents");
            return null;
        }

        /// <summary>
        /// Gets the value of a private field in an instance.
        /// </summary>
        /// <typeparam name="T">The type of the private field</typeparam>
        /// <param name="obj">The instance</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="recurse">Should the base types be examined</param>
        /// <returns></returns>
        public static T GetPrivateField<T>(this object obj, string fieldName, bool recurse = false)
        {
            var type = obj.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    return (T)field
                                  .GetValue(obj);

                if (!recurse)
                    break;
                type = type.BaseType;
            }
            Log.Error($"Field {fieldName} not found in type {obj.GetType().FullName}" + (recurse ? " or its parents" : ""));
            return default(T);
        }

        /// <summary>
        /// Gets the list of all delegates registered in the named static event
        /// </summary>
        /// <param name="type">The type (or child type) that contains the event</param>
        /// <param name="eventName">Name of the event</param>
        /// <returns>All delegates registered with the event</returns>
        public static IEnumerable<Delegate> GetStaticEvent(Type type, string eventName)
        {
            return GetEventsInternal(null, eventName, type);
        }

        /// <summary>
        /// Gets the list of all delegates registered in the named event
        /// </summary>
        /// <param name="instance">Instance to retrieve the event list for</param>
        /// <param name="eventName">Name of the event</param>
        /// <returns>All delegates registered with the event</returns>
        public static IEnumerable<Delegate> GetInstanceEvent(object instance, string eventName)
        {
            return GetEventsInternal(instance, eventName);
        }

        private static readonly string[] _backingFieldForEvent = { "{0}", "<backing_store>{0}" };
        private static IEnumerable<Delegate> GetEventsInternal(object instance, string eventName, Type baseType = null)
        {
            BindingFlags bindingFlags = BindingFlags.NonPublic |
                               (instance == null ? BindingFlags.Static : BindingFlags.Instance);

            FieldInfo eventField = null;
            baseType = baseType ?? instance?.GetType();
            Type type = baseType;
            while (type != null && eventField == null)
            {
                for (var i = 0; i < _backingFieldForEvent.Length && eventField == null; i++)
                    eventField = type.GetField(string.Format(_backingFieldForEvent[i], eventName), bindingFlags);
                type = type.BaseType;
            }
            if (eventField?.GetValue(instance) is MulticastDelegate eventDel)
            {
                foreach (Delegate handle in eventDel.GetInvocationList())
                    yield return handle;
            }
            else
                Log.Error($"{eventName} doesn't have a backing store in {baseType} or its parents.");
        }
    }
}
