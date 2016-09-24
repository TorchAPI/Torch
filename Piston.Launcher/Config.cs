using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Piston.Launcher
{
    public class Config
    {
        public int Version { get; set; }
        public string RemoteFilePath { get; set; }
        public string SpaceDirectory { get; set; }

        private Config()
        {
            Version = 0;
            RemoteFilePath = "ftp://athena.jimmacle.com/";
            SpaceDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64";
        }

        public static string GetConfigPath()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pistonFolder = Path.Combine(appdata, "Piston");
            if (!Directory.Exists(pistonFolder))
                Directory.CreateDirectory(pistonFolder);
            return Path.Combine(appdata, "Piston\\config.xml");
        }

        public static Config Load()
        {
            if (!File.Exists(GetConfigPath()))
                return new Config();

            XmlSerializer ser = new XmlSerializer(typeof(Config));
            using (var f = File.OpenRead(GetConfigPath()))
            {
                using (var sr = new StreamReader(f))
                {
                    return (Config)ser.Deserialize(sr);
                }
            }
        }

        public void Save()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Config));
            using (var sw = new StreamWriter(GetConfigPath()))
            {
                ser.Serialize(sw, this);
            }
        }
    }
}
