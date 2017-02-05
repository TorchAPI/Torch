using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Torch
{
	public static class Reflection
	{
		private static readonly Logger Log = LogManager.GetLogger("Reflection");

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
						Log.Error( "Failed to find method '" + methodName + "' in type '" + type.FullName + "'" );
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
						Log.Error( "Failed to find method '" + methodName + "' in type '" + type.FullName + "'" );
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
				Log.Error( "Failed to find method '" + methodName + "' in type '" + type.FullName + "': " + ex.Message );
				Log.Error( ex );
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

	    public static T GetPrivateField<T>(this object obj, string fieldName)
	    {
	        var type = obj.GetType();
	        return (T)type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
	    }
	}
}
