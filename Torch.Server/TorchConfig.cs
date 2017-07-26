using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using Newtonsoft.Json;
using NLog;

namespace Torch.Server
{
    public class TorchConfig : CommandLine, ITorchConfig
    {
        private static Logger _log = LogManager.GetLogger("Config");

        public bool ShouldUpdatePlugins => (GetPluginUpdates && !NoUpdate) || ForceUpdate;
        public bool ShouldUpdateTorch => (GetTorchUpdates && !NoUpdate) || ForceUpdate;

        /// <inheritdoc />
        [Arg("instancename", "The name of the Torch instance.")]
        public string InstanceName { get; set; }

        /// <inheritdoc />
        [Arg("instancepath", "Server data folder where saves and mods are stored.")]
        public string InstancePath { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("noupdate", "Disable automatically downloading game and plugin updates.")]
        public bool NoUpdate { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("forceupdate", "Manually check for and install updates.")]
        public bool ForceUpdate { get; set; }

        /// <inheritdoc />
        [Arg("autostart", "Start the server immediately.")]
        public bool Autostart { get; set; }

        /// <inheritdoc />
        [Arg("restartoncrash", "Automatically restart the server if it crashes.")]
        public bool RestartOnCrash { get; set; }

        /// <inheritdoc />
        [Arg("nogui", "Do not show the Torch UI.")]
        public bool NoGui { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("waitforpid", "Makes Torch wait for another process to exit.")]
        public string WaitForPID { get; set; }

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
        [XmlIgnore]
        private string _path;

        public TorchConfig() : this("Torch") { }

        public TorchConfig(string instanceName = "Torch", string instancePath = null)
        {
            InstanceName = instanceName;
            InstancePath = instancePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineersDedicated");
        }

        public static TorchConfig LoadFrom(string path)
        {
            try
            {
                var ser = new XmlSerializer(typeof(TorchConfig));
                using (var f = File.OpenRead(path))
                {
                    var config = (TorchConfig)ser.Deserialize(f);
                    config._path = path;
                    return config;
                }
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
                var ser = new XmlSerializer(typeof(TorchConfig));
                using (var f = File.Create(path))
                    ser.Serialize(f, this);
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
