using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Torch
{
    public class PluginManifest
    {
        /// <summary>
        /// The display name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A unique identifier for the plugin.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// A GitHub repository in the format of Author/Repository to retrieve plugin updates.
        /// </summary>
        [Obsolete("Updates no longer check git. Updates are hosted only on torchapi.com")]
        public string Repository { get; set; }

        //xml tomfoolery
        public bool ShouldSerializeRepository() => false;

        /// <summary>
        /// The plugin version. This must include a string in the format of #[.#[.#]] for update checking purposes.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// A list of dependent plugin repositories. This may be updated to include GUIDs in the future.
        /// </summary>
        public List<PluginDependency> Dependencies { get; } = new List<PluginDependency>();

        public void Save(string path)
        {
            using (var f = File.OpenWrite(path))
            {
                var ser = new XmlSerializer(typeof(PluginManifest));
                ser.Serialize(f, this);
            }
        }

        public static PluginManifest Load(string path)
        {
            using (var f = File.OpenRead(path))
            {
                return Load(f);
            }
        }

        public static PluginManifest Load(Stream stream)
        {
            var ser = new XmlSerializer(typeof(PluginManifest));
            return (PluginManifest)ser.Deserialize(stream);
        }

        public static PluginManifest Load(TextReader reader)
        {
            var ser = new XmlSerializer(typeof(PluginManifest));
            return (PluginManifest)ser.Deserialize(reader);
        }
    }
}
