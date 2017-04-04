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
    public class TorchConfig
    {
        private static Logger _log = LogManager.GetLogger("Config");

        public string InstancePath { get; set; }
        public string InstanceName { get; set; }
        public int Autosave { get; set; }
        public bool AutoRestart { get; set; }
        public bool LogChat { get; set; }

        public TorchConfig() : this("Torch") { }

        public TorchConfig(string instanceName = "Torch", string instancePath = null, int autosaveInterval = 5, bool autoRestart = false)
        {
            InstanceName = instanceName;
            InstancePath = instancePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineersDedicated", InstanceName);
            Autosave = autosaveInterval;
            AutoRestart = autoRestart;
        }

        public static TorchConfig LoadFrom(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(TorchConfig));
                TorchConfig config;
                using (var f = File.OpenRead(path))
                {
                    config = (TorchConfig)serializer.Deserialize(f);
                }
                return config;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return null;
            }
        }

        public bool SaveTo(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(TorchConfig));
                using (var f = File.OpenWrite(path))
                {
                    serializer.Serialize(f, this);
                }
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return false;
            }
        }
    }
}
