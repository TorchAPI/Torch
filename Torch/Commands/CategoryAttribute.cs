using System;
using System.Collections.Generic;
using System.Linq;

namespace Torch.Commands
{
    public class CategoryAttribute : Attribute
    {
        public List<string> Path { get; }

        [Obsolete("Use the other CategoryAttribute constructor.")]
        public CategoryAttribute(params string[] path)
        {
            Path = path.Select(i => i.ToLower()).ToList();
        }

        /// <summary>
        /// Provides information about where to place commands in the command tree. Supports space-delimited hierarchy.
        /// </summary>
        /// <param name="category"></param>
        public CategoryAttribute(string category)
        {
            Path = category.Split(' ').ToList();
        }
    }
}
