using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Commands
{
    public class CategoryAttribute : Attribute
    {
        public string[] Path { get; }

        /// <summary>
        /// Specifies where to add the class's commands in the command tree.
        /// </summary>
        /// <param name="path">Command path, e.g. "/admin config" -> "admin, config"</param>
        public CategoryAttribute(params string[] path)
        {
            Path = path;
        }
    }
}
