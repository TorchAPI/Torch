using System.Linq;
using System.Text.RegularExpressions;
using Torch.API;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public struct CommandContext
    {
        public ITorchPlugin Plugin { get; }
        public ITorchBase Torch { get; }
        public IMyPlayer Player { get; }
        public string[] Args { get; }

        /// <summary>
        /// Splits the argument by single words and quoted blocks.
        /// </summary>
        /// <returns></returns>
        public CommandContext(ITorchBase torch, ITorchPlugin plugin, IMyPlayer player, params string[] args)
        {
            Torch = torch;
            Plugin = plugin;
            Player = player;
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