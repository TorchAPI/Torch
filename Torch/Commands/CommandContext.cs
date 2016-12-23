using System.Linq;
using System.Text.RegularExpressions;

namespace Torch.Commands
{
    public struct CommandContext
    {
        public string Argument;
        public ulong SteamId;

        /// <summary>
        /// Splits the argument by single words and quoted blocks.
        /// </summary>
        /// <returns></returns>
        public string[] SplitArgument()
        {
            var split = Regex.Split(Argument, "(\"[^\"]+\"|\\S+)");
            for (var i = 0; i < split.Length; i++)
            {
                split[i] = Regex.Replace(split[i], "\"", "");
            }

            return split;
        }
    }
}