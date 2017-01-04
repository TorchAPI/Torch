using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Plugins;

namespace Torch.API
{
    public interface IPluginManager : IEnumerable<ITorchPlugin>
    {
        void UpdatePlugins();
        void LoadPlugins();
        void UnloadPlugins();
    }
}