using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Piston;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.World;
using SpaceEngineers.Game;
using VRage.Dedicated;
using VRage.Game;
using VRage.Game.SessionComponents;
using VRage.Profiler;

namespace Piston.Server
{
    public class ServerManager : IDisposable
    {
        public Thread ServerThread { get; private set; }
        public string[] RunArgs { get; set; } = new string[0];
        public bool IsRunning { get; private set; }

        public event Action SessionLoading;
        public event Action SessionLoaded;

        private readonly ManualResetEvent _stopHandle = new ManualResetEvent(false);

        internal ServerManager()
        {
            MySession.OnLoading += OnSessionLoading;
        }

        public void InitSandbox()
        {
            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();
            MyPerGameSettings.SendLogToKeen = false;
            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;
            MyPerServerSettings.GameDSName = MyPerServerSettings.GameNameSafe + "Dedicated";
            MyPerServerSettings.GameDSDescription = "Your place for space engineering, destruction and exploring.";
            MySessionComponentExtDebug.ForceDisable = true;
            MyPerServerSettings.AppId = 244850u;
            ConfigForm<MyObjectBuilder_SessionSettings>.GameAttributes = Game.SpaceEngineers;
            ConfigForm<MyObjectBuilder_SessionSettings>.OnReset = delegate
            {
                SpaceEngineersGame.SetupBasicGameInfo();
                SpaceEngineersGame.SetupPerGameSettings();
            };
            int? gameVersion = MyPerGameSettings.BasicGameInfo.GameVersion;
            MyFinalBuildConstants.APP_VERSION = gameVersion ?? 0;
        }

        /// <summary>
        /// Invokes an action on the game thread and blocks until completion
        /// </summary>
        /// <param name="action"></param>
        public void GameAction(Action action)
        {
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

        private void OnSessionLoading()
        {
            SessionLoading?.Invoke();
            MySession.Static.OnReady += OnSessionReady;
        }

        private void OnSessionReady()
        {
            SessionLoaded?.Invoke();
        }

        /// <summary>
        /// Start server on a new thread.
        /// </summary>
        public void StartServerThread()
        {
            if (ServerThread?.IsAlive ?? false)
            {
                Logger.Write("Cannot start the server because it's already running.");
                return;
            }

            ServerThread = new Thread(StartServer);
            ServerThread.Start();
        }

        /// <summary>
        /// Start server on the current thread.
        /// </summary>
        public void StartServer()
        {
            IsRunning = true;
            Logger.Write("Starting server.");

            if (MySandboxGame.Log.LogEnabled)
                MySandboxGame.Log.Close();

            DedicatedServer.Run<MyObjectBuilder_SessionSettings>(RunArgs);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void StopServer()
        {
            if (Thread.CurrentThread.ManagedThreadId != ServerThread?.ManagedThreadId)
            {
                Logger.Write("Requesting server stop.");
                MySandboxGame.Static.Invoke(StopServer);
                _stopHandle.WaitOne();
                return;
            }

            Logger.Write("Stopping server.");
            MySession.Static.Save();
            MySession.Static.Unload();
            MySandboxGame.Static.Exit();

            //Unload all the static junk.
            //TODO: Finish unloading all server data so it's in a completely clean state.
            VRage.FileSystem.MyFileSystem.Reset();
            VRage.Input.MyGuiGameControlsHelpers.Reset();
            VRage.Input.MyInput.UnloadData();
            CleanupProfilers();

            Logger.Write("Server stopped.");
            _stopHandle.Set();
            IsRunning = false;
        }

        private void CleanupProfilers()
        {
            typeof(MyRenderProfiler).GetField("m_threadProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            typeof(MyRenderProfiler).GetField("m_gpuProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            (typeof(MyRenderProfiler).GetField("m_threadProfilers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as List<MyProfiler>).Clear();
        }

        public void Dispose()
        {
            if (IsRunning)
                StopServer();
        }
    }
}
