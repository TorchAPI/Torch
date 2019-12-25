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
        public PluginManifest Manifest { get; internal set; }
        public Guid Id => Manifest.Guid;
        public string Version => Manifest.Version;
        public string Name => Manifest.Name;
        public ITorchBase Torch { get; internal set; }

        public virtual void Init(ITorchBase torch)
        {
            Torch = torch;
        }

        public virtual void Update() { }
        public PluginState State { get; }

        public virtual void Dispose() { }
    }
}
