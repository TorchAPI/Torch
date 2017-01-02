using System.Linq;
using System.Text.RegularExpressions;

namespace Torch.Commands
{
    public struct CommandContext
    {
        public string[] Args;
        public ulong SteamId;

        /// <summary>
        /// Splits the argument by single words and quoted blocks.
        /// </summary>
        /// <returns></returns>
        public CommandContext(ulong steamId, params string[] args)
        {
            SteamId = steamId;
            Args = args;

            /*
            var split = Regex.Split(Args, "(\"[^\"]+\"|\\S+)");
            for (var i = 0; i < split.Length; i++)
            {
                split[i] = Regex.Replace(split[i], "\"", "");
            }

            return split;*/
        }
    }
}