using System;

namespace Torch.Commands
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string HelpText { get; }
        public string[] Path { get; }

        public CommandAttribute(string name, string description = "", string helpText = "", params string[] path)
        {
            Name = name;
            Description = description;
            HelpText = helpText;
            Path = path.Length > 0 ? path : new [] {name.ToLower()};
        }
    }
}