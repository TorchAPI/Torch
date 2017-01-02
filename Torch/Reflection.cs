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
		public static bool HasMethod( Type objectType, string methodName )
		{
			return Reflection.HasMethod( objectType, methodName, null );
		}


		public static bool HasMethod( Type objectType, string methodName, Type[ ] argTypes )
		{
			try
			{
				if ( String.IsNullOrEmpty( methodName ) )
					return false;

				if ( argTypes == null )
				{
					MethodInfo method = objectType.GetMethod( methodName );
					if ( method == null )
						method = objectType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
					if ( method == null && objectType.BaseType != null )
						method = objectType.BaseType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
					if ( method == null )
					{
						//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "'" );
						return false;
					}
				}
				else
				{
					MethodInfo method = objectType.GetMethod( methodName, argTypes );
					if ( method == null )
						method = objectType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null );
					if ( method == null && objectType.BaseType != null )
						method = objectType.BaseType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null );
					if ( method == null )
					{
						//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "'" );
						return false;
					}
				}

				return true;
			}
			catch ( AmbiguousMatchException aex )
			{
				return true;
			}
			catch ( Exception ex )
			{
				//Log.Error( "Failed to find method '" + methodName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}

		public static bool HasField( Type objectType, string fieldName )
		{
			try
			{
				if ( String.IsNullOrEmpty( fieldName ) )
					return false;
				FieldInfo field = objectType.GetField( fieldName );
				if ( field == null )
					field = objectType.GetField( fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				if ( field == null )
					field = objectType.BaseType.GetField( fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				if ( field == null )
				{
					//Log.Error( "Failed to find field '" + fieldName + "' in type '" + objectType.FullName + "'" );
					return false;
				}
				return true;
			}
			catch ( Exception ex )
			{
				//Log.Error( "Failed to find field '" + fieldName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}

		public static bool HasProperty( Type objectType, string propertyName )
		{
			try
			{
				if ( String.IsNullOrEmpty( propertyName ) )
					return false;
				PropertyInfo prop = objectType.GetProperty( propertyName );
				if ( prop == null )
					prop = objectType.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				if ( prop == null )
					prop = objectType.BaseType.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				if ( prop == null )
				{
					//Log.Error( "Failed to find property '" + propertyName + "' in type '" + objectType.FullName + "'" );
					return false;
				}
				return true;
			}
			catch ( Exception ex )
			{
				//Log.Error( "Failed to find property '" + propertyName + "' in type '" + objectType.FullName + "': " + ex.Message );
				//Log.Error( ex );
				return false;
			}
		}
	}
}
