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
using Sandbox.Game.World;
using VRage.Profiler;

namespace Piston.Server
{
    public class ServerManager
    {
        public Thread ServerThread { get; private set; }
        public string[] RunArgs { get; set; } = new string[0];
        public bool Running { get; private set; }

        public event Action SessionLoading;
        public event Action SessionLoaded;

        private readonly Assembly _dsAssembly;
        private readonly ManualResetEvent _stopHandle = new ManualResetEvent(false);

        internal ServerManager()
        {
            using (var f = File.OpenRead("SpaceEngineersDedicated.exe"))
            {
                var bin = new byte[f.Length];
                f.Read(bin, 0, (int)f.Length);
                _dsAssembly = Assembly.Load(bin);
            }

            ServerThread = new Thread(StartServer);
            MySession.OnLoading += OnSessionLoading;
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
            if (ServerThread.IsAlive)
            {
                throw new InvalidOperationException("The server thread is already running.");
            }

            ServerThread = new Thread(StartServer);
            ServerThread.Start();
        }

        /// <summary>
        /// Start server on the current thread.
        /// </summary>
        public void StartServer()
        {
            Running = true;
            Logger.Write("Starting server.");

            if (MySandboxGame.Log.LogEnabled)
                MySandboxGame.Log.Close();

            foreach (var type in _dsAssembly.GetTypes())
            {
                if (type.FullName.Contains("MyProgram"))
                {
                    var method = type.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
                    method.Invoke(null, new object[] {RunArgs});

                    break;
                }
            }
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void StopServer()
        {
            if (Thread.CurrentThread.ManagedThreadId != ServerThread.ManagedThreadId)
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
            Running = false;
        }

        private void CleanupProfilers()
        {
            typeof(MyRenderProfiler).GetField("m_threadProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            typeof(MyRenderProfiler).GetField("m_gpuProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            (typeof(MyRenderProfiler).GetField("m_threadProfilers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as List<MyProfiler>).Clear();
        }
    }
}
