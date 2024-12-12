using System.Collections.Generic;
using Sandbox.Game.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Utils;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

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
        /// The player who ran the command, or null if the server sent it.
        /// </summary>
        public IMyPlayer Player => Torch.CurrentSession.KeenSession.Players.TryGetPlayerBySteamId(_steamIdSender);

        /// <summary>
        /// Was this message sent by this program.
        /// </summary>
        public bool SentBySelf => _steamIdSender == Sync.MyId;

        private ulong _steamIdSender;

        /// <summary>
        /// The command arguments split by spaces and quotes. Ex. "this is" a command -> {this is, a, command}
        /// </summary>
        public List<string> Args { get; }

        /// <summary>
        /// The non-split argument string.
        /// </summary>
        public string RawArgs { get; }

        public CommandContext(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null,
            List<string> args = null)
        {
            Torch = torch;
            Plugin = plugin;
            _steamIdSender = steamIdSender;
            RawArgs = rawArgs;
            Args = args ?? new List<string>();
        }

        public void Respond(string message, Color color, string sender = null, string font = null)
        {
            var chat = Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
            
            if (color == default && font != null)
                color = ColorUtils.TranslateColor(font);
            
            if (font == null)
                font = MyFontEnum.White;

            chat?.SendMessageAsOther(sender, message, color, _steamIdSender, font);
        }

        public virtual void Respond(string message, string sender = null, string font = null)
        {
            //hack: Backwards compatibility 20190416
            if (sender == "Server")
            {
                sender = null;
                font = null;
            }
            
            Respond(message, default, sender, font);
        }
    }
}