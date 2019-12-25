using System;

namespace Torch
{
    public class PluginDependency
    {
        /// <summary>
        /// A unique identifier for the plugin that identifies the dependency.
        /// </summary>
        public Guid Plugin { get; set; } 
        
        /// <summary>
        /// The plugin minimum version. This must include a string in the format of #[.#[.#]] for update checking purposes.
        /// </summary>
        public string MinVersion { get; set; }
    }
}