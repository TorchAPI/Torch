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
        /// <summary>
        /// The plugin that added this command.
        /// </summary>
        public ITorchPlugin Plugin { get; }
        /// <summary>
        /// The current Torch instance.
        /// </summary>
        public ITorchBase Torch { get; }
        /// <summary>
        /// The player who ran the command.
        /// </summary>
        public IMyPlayer Player { get; }
        /// <summary>
        /// The command arguments split by spaces and quotes. Ex. "this is" a command -> {this is, a, command}
        /// </summary>
        public List<string> Args { get; }

        public CommandContext(ITorchBase torch, ITorchPlugin plugin, IMyPlayer player, List<string> args = null)
        {
            Torch = torch;
            Plugin = plugin;
            Player = player;
            Args = args ?? new List<string>();
        }

        public void Respond(string message, string sender = "Server", string font = MyFontEnum.Blue)
        {
            Torch.Multiplayer.SendMessage(message, sender, Player.IdentityId, font);
        }
    }
}