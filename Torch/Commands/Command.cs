using System;

namespace Torch.Commands
{
    public class Command
    {
        public CommandModule Module;
        public string Name;
        public string[] Path;
        public Action<CommandContext> Invoke;
    }
}