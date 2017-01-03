using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;

namespace Torch
{
    public abstract class TorchPluginBase : ITorchPlugin
    {
        public Guid Id { get; }
        public Version Version { get; }
        public string Name { get; }
        public ITorchBase Torch { get; private set; }

        protected TorchPluginBase()
        {
            var asm = Assembly.GetCallingAssembly();

            var id = asm.GetCustomAttribute<GuidAttribute>()?.Value;
            if (id == null)
                throw new InvalidOperationException($"{asm.FullName} has no Guid attribute.");

            Id = new Guid(id);

            var ver = asm.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
            if (ver == null)
                throw new InvalidOperationException($"{asm.FullName} has no AssemblyVersion attribute.");

            Version = new Version(ver);

            var name = asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            if (name == null)
                throw new InvalidOperationException($"{asm.FullName} has no AssemblyTitle attribute.");

            Name = name;
        }

        public virtual void Init(ITorchBase torch)
        {
            Torch = torch;
        }

        public abstract void Update();
        public abstract void Unload();
    }
}
