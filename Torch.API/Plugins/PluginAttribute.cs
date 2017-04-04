using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Plugins
{
    public class PluginAttribute : Attribute
    {
        public string Name { get; }
        public Version Version { get; }
        public Guid Guid { get; }

        public PluginAttribute(string name, string version, string guid)
        {
            Name = name;
            Version = Version.Parse(version);
            Guid = Guid.Parse(guid);
        }
    }
}
