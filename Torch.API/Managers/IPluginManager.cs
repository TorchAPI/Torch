using System;
using System.Collections.Generic;
using Torch.API.Plugins;
using VRage.Collections;
using VRage.Plugins;

namespace Torch.API.Managers
{
    public interface IPluginManager : IManager, IEnumerable<ITorchPlugin>
    {
        event Action<List<ITorchPlugin>> PluginsLoaded;
        List<ITorchPlugin> Plugins { get; }
        void UpdatePlugins();
        void DisposePlugins();
    }
}