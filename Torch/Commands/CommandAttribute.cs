using System;

namespace Torch.Commands
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }

        /// <summary>
        /// Specifies a command to add to the command tree.
        /// </summary>
        /// <param name="name"></param>
        public CommandAttribute(string name)
        {
            Name = name;
        }
    }
}