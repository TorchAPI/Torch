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

namespace PistonServer
{
    public class ServerManager
    {
        public static ServerManager Static { get; } = new ServerManager();
        public Thread ServerThread { get; private set; }
        public string[] RunArgs { get; set; } = new string[0];

        private readonly Assembly _dsAssembly;

        private ServerManager()
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
            MySession.Static.OnReady += OnSessionReady;
        }

        private void OnSessionReady()
        {
            MyMultiplayer.Static.ChatMessageReceived += Program.UserInterface.Chat.MessageReceived;
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
        /// <param name="abortThread"></param>
        public void StopServer(bool abortThread = false)
        {
            if (Thread.CurrentThread != ServerThread)
            {
                MySandboxGame.Static?.Invoke(() => StopServer(true));
                return;
            }

            MySandboxGame.Static.Exit();

            //Unload all the static junk.
            //TODO: Finish unloading all server data so it's in a completely clean state.
            VRage.FileSystem.MyFileSystem.Reset();
            VRage.Input.MyGuiGameControlsHelpers.Reset();
            VRage.Input.MyInput.UnloadData();
            CleanupProfilers();
            GC.Collect(2);

            Logger.Write("Server stopped.");
            if (abortThread)
            {
                try { ServerThread.Abort(); }
                catch (ThreadAbortException)
                {
                    Logger.Write("Server thread aborted.");
                }
                ServerThread = null;
            }
        }

        private void CleanupProfilers()
        {
            typeof(MyRenderProfiler).GetField("m_threadProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            typeof(MyRenderProfiler).GetField("m_gpuProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            (typeof(MyRenderProfiler).GetField("m_threadProfilers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as List<MyProfiler>).Clear();
        }
    }
}
