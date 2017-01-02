using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public class PluginAttribute : Attribute
    {
        public string Name { get; }
        public bool Reloadable { get; }

        public PluginAttribute(string name, bool reloadable = false)
        {
            Name = name;
            Reloadable = reloadable;
        }
    }
}
