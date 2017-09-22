using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Torch
{
    public static class StringExtensions
    {
        /// <summary>
        /// Try to extract a 3 component version from the string. Format: #.#.#
        /// </summary>
        public static bool TryExtractVersion(this string version, out Version result)
        {
            result = null;
            var match = Regex.Match(version, @"(\d+\.)?(\d+\.)?(\d+)");
            return match.Success && Version.TryParse(match.Value, out result);
        }
    }
}
