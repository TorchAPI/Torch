using System;
using System.Collections.Generic;
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
        public IPluginManager Plugins { get; protected set; }
        public IMultiplayer Multiplayer { get; protected set; }

        public event Action SessionLoaded;

        protected void InvokeSessionLoaded()
        {
            SessionLoaded?.Invoke();
        }

        protected TorchBase()
        {
            Plugins = new PluginManager();
            Multiplayer = new MultiplayerManager(this);
        }

        /// <summary>
        /// Invokes an action on the game thread and blocks until completion
        /// </summary>
        /// <param name="action"></param>
        public void GameAction(Action action)
        {
            if (action == null)
                return;

            try
            {
                if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
                {
                    action();
                }
                else
                {
                    AutoResetEvent e = new AutoResetEvent(false);

                    MySandboxGame.Static.Invoke(() =>
                    {
                        try
                        {
                            action();
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

                    //timeout so we don't accidentally hang the server
                    e.WaitOne(60000);
                }
            }
            catch (Exception ex)
            {
                //we need a logger :(
            }
        }

        /// <summary>
        /// Queues an action for invocation on the game thread and optionally runs a callback on completion
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public void BeginGameAction(Action action, Action<object> callback = null, object state = null)
        {
            if (action == null)
                return;

            try
            {
                if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
                {
                    action();
                }
                else
                {
                    Task.Run(() =>
                    {
                        GameAction(action);
                        callback?.Invoke(state);
                    });
                }
            }
            catch (Exception ex)
            {
                // log
            }
        }

        public abstract void Start();
        public abstract void Stop();
        public abstract void Init();
    }
}
