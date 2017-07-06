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
        public string Repository { get; set; } = "Jimmacle/notarealrepo";
        public string Version { get; set; } = "1.0";

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
                var ser = new XmlSerializer(typeof(PluginManifest));
                return (PluginManifest)ser.Deserialize(f);
            }
        }
    }
}
