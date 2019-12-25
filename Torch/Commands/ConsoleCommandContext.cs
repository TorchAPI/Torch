using System.Collections.Generic;
using System.Text;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;

namespace Torch.Commands
{
    public class ConsoleCommandContext : CommandContext
    {
        public List<TorchChatMessage> Responses = new List<TorchChatMessage>();
        private bool _flag;
        
        /// <inheritdoc />
        public ConsoleCommandContext(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null, List<string> args = null) 
            : base(torch, plugin, steamIdSender, rawArgs, args) { }

        /// <inheritdoc />
        public override void Respond(string message, string sender = null, string font = null)
        {
            if (sender == "Server")
            {
                sender = null;
                font = null;
            }
            
            Responses.Add(new TorchChatMessage(sender ?? TorchBase.Instance.Config.ChatName, message, font ?? TorchBase.Instance.Config.ChatColor));
        }
    }
}