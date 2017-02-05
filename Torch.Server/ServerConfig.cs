using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using VRage.Dedicated;

namespace Torch.Server
{
    public class ServerConfig
    {
        private static Logger _log = LogManager.GetLogger("Config");

        public string InstancePath { get; set; }
        public string InstanceName { get; set; }
        //public string SaveName { get; set; }
        public int Autosave { get; set; }
        public bool AutoRestart { get; set; }

        public ServerConfig(string instanceName = "Torch", string instancePath = null, int autosaveInterval = 5, bool autoRestart = false)
        {
            InstanceName = instanceName;
            InstancePath = instancePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Torch", InstanceName);
            Autosave = autosaveInterval;
            AutoRestart = autoRestart;
        }

        public static ServerConfig LoadFrom(string path)
        {
            _log.Info($"Loading config from '{path}'");
            try
            {
                var serializer = new XmlSerializer(typeof(ServerConfig));
                ServerConfig config;
                using (var f = File.OpenRead(path))
                {
                    config = (ServerConfig)serializer.Deserialize(f);
                }
                return config;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public bool SaveTo(string path)
        {
            _log.Info($"Saving config to '{path}'");
            try
            {
                var serializer = new XmlSerializer(typeof(ServerConfig));
                using (var f = File.OpenWrite(path))
                {
                    serializer.Serialize(f, this);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
