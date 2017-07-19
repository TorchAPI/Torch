using System;
using System.Collections.Generic;
using Torch.API.Plugins;
using VRage.Collections;
using VRage.Plugins;

namespace Torch.API.Managers
{
    /// <summary>
    /// API for the Torch plugin manager.
    /// </summary>
    public interface IPluginManager : IManager, IEnumerable<ITorchPlugin>
    {
        /// <summary>
        /// Fired when plugins are loaded.
        /// </summary>
        event Action<IList<ITorchPlugin>> PluginsLoaded;

        /// <summary>
        /// Collection of loaded plugins.
        /// </summary>
        IList<ITorchPlugin> Plugins { get; }

        /// <summary>
        /// Updates all loaded plugins.
        /// </summary>
        void UpdatePlugins();

        /// <summary>
        /// Disposes all loaded plugins.
        /// </summary>
        void DisposePlugins();

        /// <summary>
        /// Load plugins.
        /// </summary>
        void LoadPlugins();
    }
}