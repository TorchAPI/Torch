using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using NLog;

namespace Torch.Server
{
    public class TorchConfig : CommandLine, ITorchConfig
    {
        private static Logger _log = LogManager.GetLogger("Config");

        /// <inheritdoc />
        [Arg("instancepath", "Server data folder where saves and mods are stored.")]
        public string InstancePath { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("noupdate", "Disable automatically downloading game and plugin updates.")]
        public bool NoUpdate { get => false; set => GetTorchUpdates = GetPluginUpdates = !value; }

        /// <inheritdoc />
        [XmlIgnore, Arg("update", "Manually check for and install updates.")]
        public bool Update { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("autostart", "Start the server immediately.")]
        public bool Autostart { get; set; }

        /// <inheritdoc />
        [Arg("restartoncrash", "Automatically restart the server if it crashes.")]
        public bool RestartOnCrash { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("nogui", "Do not show the Torch UI.")]
        public bool NoGui { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("waitforpid", "Makes Torch wait for another process to exit.")]
        public string WaitForPID { get; set; }

        /// <inheritdoc />
        [Arg("instancename", "The name of the Torch instance.")]
        public string InstanceName { get; set; }

        /// <inheritdoc />
        public bool GetTorchUpdates { get; set; } = true;

        /// <inheritdoc />
        public bool GetPluginUpdates { get; set; } = true;

        /// <inheritdoc />
        public int TickTimeout { get; set; } = 60;

        /// <inheritdoc />
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
