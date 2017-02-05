using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Torch.API;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class CommandContext
    {
        public ITorchPlugin Plugin { get; }
        public ITorchBase Torch { get; }
        public IMyPlayer Player { get; }
        public List<string> Args { get; }

        /// <summary>
        /// Splits the argument by single words and quoted blocks.
        /// </summary>
        /// <returns></returns>
        public CommandContext(ITorchBase torch, ITorchPlugin plugin, IMyPlayer player, List<string> args = null)
        {
            Torch = torch;
            Plugin = plugin;
            Player = player;
            Args = args ?? new List<string>();
        }

        public void Respond(string message, string sender = "Server", string font = MyFontEnum.Blue)
        {
            Torch.Multiplayer.SendMessage(message, Player.IdentityId, sender, font);
        }
    }
}