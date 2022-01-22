using System;
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
        public event Action<TorchChatMessage> OnResponse;

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

#pragma warning disable CS0618
#pragma warning disable CS0612
            var msg = new TorchChatMessage(sender ?? TorchBase.Instance.Config.ChatName, message, font ?? TorchBase.Instance.Config.ChatColor);
#pragma warning restore CS0612
#pragma warning restore CS0618
            Responses.Add(msg);
            OnResponse?.Invoke(msg);
        }
    }
}