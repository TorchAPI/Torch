using System;
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
        public bool IsReloadable { get; set; }
        public ITorchBase Torch { get; internal set; }

        public virtual void Init(ITorchBase torch)
        {
            Torch = torch;
        }

        public virtual void Update() { }
        public PluginState State { get; } = PluginState.Enabled;

        public virtual void Dispose() { }
    }
}
