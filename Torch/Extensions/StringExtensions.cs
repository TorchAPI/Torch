using System;
using System.Text.RegularExpressions;

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
            var match = Regex.Match(version, @"(\d+\.)?(\d+\.)?(\d+\.)?(\d+)");
            return match.Success && Version.TryParse(match.Value, out result);
        }
    }
}
