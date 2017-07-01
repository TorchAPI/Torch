using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using NLog;

namespace Torch.Server
{
    public class TorchConfig : ITorchConfig
    {
        private static Logger _log = LogManager.GetLogger("Config");

        public string InstancePath { get; set; }
        public string InstanceName { get; set; }
#warning World Path not implemented
        public string WorldPath { get; set; }
        public bool AutomaticUpdates { get; set; } = true;
        public bool RedownloadPlugins { get; set; }
        public bool RestartOnCrash { get; set; }
        /// <summary>
        /// How long in seconds to wait before automatically resetting a frozen server.
        /// </summary>
        public int TickTimeout { get; set; } = 60;
        /// <summary>
        /// A list of plugins to install or update. TODO
        /// </summary>
        public List<string> Plugins { get; set; } = new List<string>();
        internal Point WindowSize { get; set; } = new Point(800, 600);
        internal Point WindowPosition { get; set; } = new Point();
        [NonSerialized]
        private string _path;

        public TorchConfig() : this("Torch") { }

        public TorchConfig(string instanceName = "Torch", string instancePath = null, int autosaveInterval = 5, bool autoRestart = false)
        {
            InstanceName = instanceName;
            InstancePath = instancePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineersDedicated");
            //Autosave = autosaveInterval;
            //AutoRestart = autoRestart;
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
                config._path = path;
                return config;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return null;
            }
        }

        public bool Save(string path = null)
        {
            if (path == null)
                path = _path;
            else
                _path = path;

            try
            {
                var serializer = new XmlSerializer(typeof(TorchConfig));
                using (var f = File.Create(path))
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
