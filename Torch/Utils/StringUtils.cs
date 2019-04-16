using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Utils
{
    /// <summary>
    /// Utility methods for strings
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Determines a common prefix for the given set of strings
        /// </summary>
        /// <param name="set">Set of strings</param>
        /// <returns>Common prefix</returns>
        public static string CommonPrefix(IEnumerable<string> set)
        {
            StringBuilder builder = null;
            foreach (string other in set)
            {
                if (builder == null)
                    builder = new StringBuilder(other);
                if (builder.Length > other.Length)
                    builder.Remove(other.Length, builder.Length - other.Length);
                for (var i = 0; i < builder.Length; i++)
                    if (builder[i] != other[i])
                    {
                        builder.Remove(i, builder.Length - i);
                        break;
                    }
            }
            return builder?.ToString() ?? "";
        }

        /// <summary>
        /// Determines a common suffix for the given set of strings
        /// </summary>
        /// <param name="set">Set of strings</param>
        /// <returns>Common suffix</returns>
        public static string CommonSuffix(IEnumerable<string> set)
        {
            StringBuilder builder = null;
            foreach (string other in set)
            {
                if (builder == null)
                    builder = new StringBuilder(other);
                if (builder.Length > other.Length)
                    builder.Remove(0, builder.Length - other.Length);
                for (var i = 0; i < builder.Length; i++)
                {
                    if (builder[builder.Length - 1 - i] != other[other.Length - 1 - i])
                    {
                        builder.Remove(0, builder.Length - i);
                        break;
                    }
                }
            }
            return builder?.ToString() ?? "";
        }

        private static string[] FontEnumValues => _fontEnumValues ?? (_fontEnumValues = typeof(VRage.Game.MyFontEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => x.IsLiteral && !x.IsInitOnly).Select(x => (string)x.GetValue(null)).ToArray());

        private static string[] _fontEnumValues;
        public static bool IsFontEnum(string str)
        {
            return FontEnumValues.Contains(str);
        }
    }
}
