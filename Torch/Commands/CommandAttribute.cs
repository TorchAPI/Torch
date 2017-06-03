using System;
using System.Collections.Generic;
using System.Linq;

namespace Torch.Commands
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string HelpText { get; }
        public List<string> Path { get; } = new List<string>();

        [Obsolete("Use the other CommandAttribute constructor.")]
        public CommandAttribute(string name, string description = "", string helpText = null, params string[] path)
        {
            Name = name;
            Description = description;
            HelpText = helpText ?? description;
            
            Path.AddRange(path.Select(x => x.ToLower()));
            Path.Add(name.ToLower());
        }

        /// <summary>
        /// Provides information about the command. Supports space-delimited hierarchy.
        /// </summary>
        public CommandAttribute(string command, string description = "", string helpText = null)
        {
            var split = command.Split(' ');
            Name = split.Last();
            Description = description;
            HelpText = helpText ?? description;

            Path.AddRange(split);
        }
    }
}