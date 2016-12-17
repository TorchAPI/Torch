using System;

namespace Torch.Commands
{
    public class ChatCommand
    {
        public ChatCommandModule Module;
        public string Name;
        public Action<CommandContext> Invoke;
    }
}