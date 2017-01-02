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
        public bool Enabled { get; set; } = true;
        public bool IsRunning => !Loop.IsCompleted;
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

        #region Internal Loop Code

        internal CancellationTokenSource ctSource = new CancellationTokenSource();

        internal Task Loop { get; private set; } = Task.CompletedTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromSeconds(1d / 60d);
        private bool _runLoop;
        internal Task Run(ITorchBase torch, bool enable = false)
        {
            if (IsRunning)
                throw new InvalidOperationException($"Plugin {Name} is already running.");

            if (!Enabled)
                return Loop = Task.CompletedTask;

            _runLoop = true;
            return Loop = Task.Run(() =>
            {
                try
                {
                    Init(torch);

                    while (Enabled && !ctSource.Token.IsCancellationRequested)
                    {
                        ctSource.Token.ThrowIfCancellationRequested();
                        var ts = Stopwatch.GetTimestamp();
                        Update();
                        var time = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - ts);

                        if (time < _loopInterval)
                            Task.Delay(_loopInterval - time);
                    }

                    Unload();
                }
                catch (Exception e)
                {
                    torch.Log.Write($"Plugin {Name} threw an exception.");
                    torch.Log.WriteException(e);
                    throw;
                }
            });
        }

        internal async Task StopAsync()
        {
            ctSource.Cancel();
            await Loop;
        }

        #endregion
    }
}
