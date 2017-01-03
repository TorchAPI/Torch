using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.World;
using SpaceEngineers.Game;
using Torch.API;
using VRage.Dedicated;
using VRage.Game;
using VRage.Game.SessionComponents;
using VRage.Profiler;

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        public Thread GameThread { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action SessionLoading;

        private readonly ManualResetEvent _stopHandle = new ManualResetEvent(false);

        internal TorchServer()
        {
            MySession.OnLoading += OnSessionLoading;
        }

        public override void Init()
        {
            base.Init();

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
            var gameVersion = MyPerGameSettings.BasicGameInfo.GameVersion;
            MyFinalBuildConstants.APP_VERSION = gameVersion ?? 0;
        }

        private void OnSessionLoading()
        {
            SessionLoading?.Invoke();
            MySession.Static.OnReady += OnSessionReady;
        }

        private void OnSessionReady()
        {
            Plugins.LoadPlugins();
            InvokeSessionLoaded();
        }

        /// <summary>
        /// Start server on the current thread.
        /// </summary>
        public override void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Server is already running.");

            IsRunning = true;
            Log.Write("Starting server.");

            if (MySandboxGame.Log.LogEnabled)
                MySandboxGame.Log.Close();

            DedicatedServer.Run<MyObjectBuilder_SessionSettings>(RunArgs);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            if (Thread.CurrentThread.ManagedThreadId != GameThread?.ManagedThreadId)
            {
                Log.Write("Requesting server stop.");
                MySandboxGame.Static.Invoke(Stop);
                _stopHandle.WaitOne();
                return;
            }

            Log.Write("Stopping server.");
            MySession.Static.Save();
            MySession.Static.Unload();
            MySandboxGame.Static.Exit();

            //Unload all the static junk.
            //TODO: Finish unloading all server data so it's in a completely clean state.
            VRage.FileSystem.MyFileSystem.Reset();
            VRage.Input.MyGuiGameControlsHelpers.Reset();
            VRage.Input.MyInput.UnloadData();
            CleanupProfilers();

            Log.Write("Server stopped.");
            _stopHandle.Set();
            IsRunning = false;
        }

        private void CleanupProfilers()
        {
            typeof(MyRenderProfiler).GetField("m_threadProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            typeof(MyRenderProfiler).GetField("m_gpuProfiler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            (typeof(MyRenderProfiler).GetField("m_threadProfilers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as List<MyProfiler>).Clear();
        }
    }
}
