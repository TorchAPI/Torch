using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using NLog;
using SteamKit2;

namespace SteamWorkshopService
{
    public static class KeyValueExtensions
    {
        private static Logger Log = LogManager.GetLogger("SteamWorkshopService");

        public static T GetValueOrDefault<T>(this KeyValue kv, string key)
        {
            kv.TryGetValueOrDefault<T>(key, out T result);
            return result;
        }
        public static bool TryGetValueOrDefault<T>(this KeyValue kv, string key, out T typedValue)
        {
            var match = kv.Children?.Find((KeyValue item) => item.Name == key);
            object result = default(T);
            if (match == null)
            {
                typedValue = (T) result;
                return false;
            }

            var value = match.Value ?? "";

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                result = converter.ConvertFromString(value);
                typedValue = (T)result;
                return true;
            }
            catch (NotSupportedException)
            {
                throw new Exception($"Unexpected Type '{typeof(T)}'!");
            }
        }
    }
}
