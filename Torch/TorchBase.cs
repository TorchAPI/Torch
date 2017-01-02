using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Torch.API;
using VRage.Scripting;

namespace Torch
{
    public abstract class TorchBase : ITorchBase
    {
        /// <summary>
        /// Dirty hack because *keen*
        /// Use only if absolutely necessary.
        /// </summary>
        public static ITorchBase Instance { get; private set; }
        public string[] RunArgs { get; set; }
        public IPluginManager Plugins { get; protected set; }
        public IMultiplayer Multiplayer { get; protected set; }
        public ILogger Log { get; set; }
        public event Action SessionLoaded;

        private bool _init;

        protected void InvokeSessionLoaded()
        {
            SessionLoaded?.Invoke();
        }

        protected TorchBase()
        {
            if (Instance != null)
                throw new InvalidOperationException("A TorchBase instance already exists.");

            Instance = this;

            Log = new Logger(Path.Combine(Directory.GetCurrentDirectory(), "TorchLog.log"));
            RunArgs = new string[0];
            Plugins = new PluginManager(this);
            Multiplayer = new MultiplayerManager(this);
        }

        /// <summary>
        /// Invokes an action on the game thread.
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action)
        {
            MySandboxGame.Static.Invoke(action);
        }

        /// <summary>
        /// Invokes an action on the game thread asynchronously.
        /// </summary>
        /// <param name="action"></param>
        public Task InvokeAsync(Action action)
        {
            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(InvokeAsync)} should not be called on the game thread.");
                action?.Invoke();
                return Task.CompletedTask;
            }

            return Task.Run(() => InvokeBlocking(action));
        }

        /// <summary>
        /// Invokes an action on the game thread and blocks until it is completed.
        /// </summary>
        /// <param name="action"></param>
        public void InvokeBlocking(Action action)
        {
            if (action == null)
                return;

            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(InvokeBlocking)} should not be called on the game thread.");
                action.Invoke();
                return;
            }

            var e = new AutoResetEvent(false);

            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    //log
                }
                finally
                {
                    e.Set();
                }
            });

            if (!e.WaitOne(60000))
                throw new TimeoutException("The game action timed out.");
        }

        public virtual void Init()
        {
            Debug.Assert(!_init, "Torch instance is already initialized.");

            _init = true;
            MyScriptCompiler.Static.AddConditionalCompilationSymbols("TORCH");
            MyScriptCompiler.Static.AddReferencedAssemblies(typeof(ITorchBase).Assembly.Location);
            MyScriptCompiler.Static.AddReferencedAssemblies(typeof(TorchBase).Assembly.Location);
        }

        public abstract void Start();
        public abstract void Stop();
    }
}
