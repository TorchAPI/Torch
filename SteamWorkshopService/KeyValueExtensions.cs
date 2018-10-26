using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var success = false;

            if (typeof(T) == typeof(UInt64))
            {
                if (success = UInt64.TryParse(value, out UInt64 uintResult))
                    result = uintResult;
                else
                    result = default(UInt64);
            }
            else if (typeof(T) == typeof(uint) || typeof(T) == typeof(UInt32) )
            {
                if (success = UInt32.TryParse(value, out UInt32 uintResult))
                    result = uintResult;
                else
                    result = default(UInt32);
            }
            else if (typeof(T) == typeof(string) || typeof(T) == typeof(String))
            {
                success = true;
                result = value;
            }
            else if (typeof(T) == typeof(Int32) || typeof(T) == typeof(int))
            {
                if (success = Int32.TryParse(value, out int intResult))
                    result = intResult;
                else
                    result = default(int);
            }
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(Int64))
            {
                if (success = Int64.TryParse(value, out long longResult))
                    result = longResult;
                else
                    result = default(long);
            }
            else
            {
                throw new Exception($"Unexpected Type '{typeof(T)}'!");
            }

            typedValue = (T)result;

            return true;
        }
    }
}
