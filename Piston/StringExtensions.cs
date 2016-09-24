using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piston
{
    public static class StringExtensions
    {
        public static string Truncate(this string s, int maxLength)
        {
            return s.Length <= maxLength ? s : s.Substring(0, maxLength);
        }

        public static IEnumerable<string> ReadLines(this string s, int max, bool skipEmpty = false, char delim = '\n')
        {
            var lines = s.Split(delim);

            for (var i = 0; i < lines.Length && i < max; i++)
            {
                var l = lines[i];
                if (skipEmpty && string.IsNullOrWhiteSpace(l))
                    continue;

                yield return l;
            }
        }

        public static string Wrap(this string s, int lineLength)
        {
            if (s.Length <= lineLength)
                return s;

            var result = new StringBuilder();
            for (var i = 0; i < s.Length;)
            {
                var next = i + lineLength;
                if (s.Length - 1 < next)
                {
                    result.AppendLine(s.Substring(i));
                    break;
                }

                result.AppendLine(s.Substring(i, next));
                i = next;
            }

            return result.ToString();
        }
    }
}
