using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch
{
	public static class Reflection
	{
		//private static readonly Logger Log = LogManager.GetLogger( "BaseLog" );

		public static bool HasMethod(Type objectType, string methodName, Type[] argTypes = null)
		{
			try
			{
				if (string.IsNullOrEmpty(methodName))
					return false;

				if (argTypes == null)
				{
					var methodInfo = objectType.GetMethod(methodName);
					if (methodInfo == null)
						methodInfo = objectType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					if (methodInfo == null && objectType.BaseType != null)
						methodInfo = objectType.BaseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					if (methodInfo == null)
					{
						//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "'" );
						return false;
					}
				}
				else
				{
					MethodInfo method = objectType.GetMethod(methodName, argTypes);
					if (method == null)
						method = objectType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
					if (method == null && objectType.BaseType != null)
						method = objectType.BaseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
					if (method == null)
					{
						//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "'" );
						return false;
					}
				}

				return true;
			}
			catch (AmbiguousMatchException aex)
			{
				return true;
			}
			catch (Exception ex)
			{
				//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}

		public static bool HasField(Type objectType, string fieldName)
		{
			try
			{
				if (string.IsNullOrEmpty(fieldName))
					return false;
				var field = objectType.GetField(fieldName);
				if (field == null)
					field = objectType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (field == null)
					field = objectType.BaseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (field == null)
				{
					//Log.Error( "Failed to find field '" + fieldName + "' in type '" + objectType.FullName + "'" );
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				//Log.Error( "Failed to find field '" + fieldName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}

		public static bool HasProperty(Type objectType, string propertyName)
		{
			try
			{
				if (string.IsNullOrEmpty(propertyName))
					return false;
				var prop = objectType.GetProperty(propertyName);
				if (prop == null)
					prop = objectType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (prop == null)
					prop = objectType.BaseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (prop == null)
				{
					//Log.Error( "Failed to find property '" + propertyName + "' in type '" + objectType.FullName + "'" );
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				//Log.Error( "Failed to find property '" + propertyName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}

	    public static object InvokeStatic(Type type, string methodName, params object[] args)
	    {
	        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	        if (method == null)
	            throw new TypeLoadException($"Method {methodName} not found in static class {type.FullName}");

	        return method.Invoke(null, args);
	    }
	}
}
