using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Torch.API;

namespace Torch
{
    public abstract class TorchBase : ITorchBase
    {
        public static ITorchBase Instance { get; private set; }

        public string[] RunArgs { get; set; }
        public IPluginManager Plugins { get; protected set; }
        public IMultiplayer Multiplayer { get; protected set; }

        public event Action SessionLoaded;

        protected void InvokeSessionLoaded()
        {
            SessionLoaded?.Invoke();
        }

        protected TorchBase()
        {
            RunArgs = new string[0];
            Instance = this;
            Plugins = new PluginManager();
            Multiplayer = new MultiplayerManager(this);
        }

        public void DoGameAction(Action action)
        {
            MySandboxGame.Static.Invoke(action);
        }

        /// <summary>
        /// Invokes an action on the game thread asynchronously.
        /// </summary>
        /// <param name="action"></param>
        public Task DoGameActionAsync(Action action)
        {
            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(DoGameActionAsync)} should not be called on the game thread.");
                action?.Invoke();
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                var e = new AutoResetEvent(false);

                MySandboxGame.Static.Invoke(() =>
                {
                    try
                    {
                        action?.Invoke();
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

                if(!e.WaitOne(60000))
                    throw new TimeoutException("The game action timed out.");
                
            });
        }

        public abstract void Start();
        public abstract void Stop();
        public abstract void Init();
    }
}
