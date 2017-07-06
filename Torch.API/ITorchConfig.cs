using System.Collections.Generic;

namespace Torch
{
    public interface ITorchConfig
    {
        /// <summary>
        /// (server) Name of the instance.
        /// </summary>
        string InstanceName { get; set; }

        /// <summary>
        /// (server) Dedicated instance path.
        /// </summary>
        string InstancePath { get; set; }

        /// <summary>
        /// Enable automatic Torch updates.
        /// </summary>
        bool GetTorchUpdates { get; set; }

        /// <summary>
        /// Enable automatic Torch updates.
        /// </summary>
        bool GetPluginUpdates { get; set; }

        /// <summary>
        /// Restart Torch automatically if it crashes.
        /// </summary>
        bool RestartOnCrash { get; set; }

        /// <summary>
        /// Time-out in seconds for the Torch watchdog (to detect a hung session).
        /// </summary>
        int TickTimeout { get; set; }

        /// <summary>
        /// A list of plugins that should be installed.
        /// </summary>
        List<string> Plugins { get; }
        
        /// <summary>
        /// Saves the config.
        /// </summary>
        bool Save(string path = null);
    }
}