using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Plugins;

namespace Torch
{
    public abstract class TorchPluginBase : ITorchPlugin
    {
        public string StoragePath { get; internal set; }
        public Guid Id { get; }
        public Version Version { get; }
        public string Name { get; }
        public ITorchBase Torch { get; private set; }
        private static readonly Logger _log = LogManager.GetLogger(nameof(TorchPluginBase));

        protected TorchPluginBase()
        {
            var type = GetType();
            var pluginInfo = type.GetCustomAttribute<PluginAttribute>();
            if (pluginInfo == null)
            {
                _log.Warn($"Plugin {type.FullName} has no PluginAttribute");
                Name = type.FullName;
                Version = new Version(0, 0, 0, 0);
                Id = default(Guid);
            }
            else
            {
                Name = pluginInfo.Name;
                Version = pluginInfo.Version;
                Id = pluginInfo.Guid;
            }
        }

        public virtual void Init(ITorchBase torch)
        {
            Torch = torch;
        }

        public virtual void Update() { }

        public virtual void Dispose() { }
    }
}
