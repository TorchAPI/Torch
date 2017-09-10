using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Torch
{
    public class PluginManifest
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public string Repository { get; set; }
        public string Version { get; set; }
        public List<Guid> Dependencies { get; } = new List<Guid>();

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
