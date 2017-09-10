using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Plugins
{
    /// <summary>
    /// Indicates that the given type should be loaded by the plugin manager as a plugin.
    /// </summary>
    [Obsolete]
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        /// <summary>
        /// The display name of the plugin
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The version of the plugin
        /// </summary>
        public Version Version { get; }
        /// <summary>
        /// The GUID of the plugin
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// Creates a new plugin attribute with the given attributes
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="guid"></param>
        public PluginAttribute(string name, string version, string guid)
        {
            Name = name;
            Version = Version.Parse(version);
            Guid = Guid.Parse(guid);
        }

        /// <summary>
        /// Creates a new plugin attribute with the given attributes.  Version is computed as the version of the assembly containing the given type.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="versionSupplier">Version is this type's assembly's version</param>
        /// <param name="guid"></param>
        public PluginAttribute(string name, Type versionSupplier, string guid)
        {
            Name = name;
            Version = versionSupplier.Assembly.GetName().Version;
            Guid = Guid.Parse(guid);
        }
    }
}
