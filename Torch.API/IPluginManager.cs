using System;
using System.Collections.Generic;
using Torch.API.Plugins;
using VRage.Collections;
using VRage.Plugins;

namespace Torch.API
{
    public interface IPluginManager : IEnumerable<ITorchPlugin>
    {
        event Action<List<ITorchPlugin>> PluginsLoaded;
        List<ITorchPlugin> Plugins { get; }
        void UpdatePlugins();
        void Init();
        void DisposePlugins();
    }
}