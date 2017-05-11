using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.World;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using SteamSDK;
using Torch.API;
using VRage.Dedicated;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilder;
using VRage.Game.SessionComponents;
using VRage.Plugins;

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        public Thread GameThread { get; private set; }
        public ServerState State { get; private set; }
        public string InstanceName => Config?.InstanceName;
        public string InstancePath => Config?.InstancePath;

        private readonly AutoResetEvent _stopHandle = new AutoResetEvent(false);

        public TorchServer(TorchConfig config = null)
        {
            Config = config ?? new TorchConfig();
        }

        public override void Init()
        {
            base.Init();

            Log.Info($"Init server '{Config.InstanceName}' at '{Config.InstancePath}'");

            MyFakes.ENABLE_INFINARIO = false;
            MyPerGameSettings.SendLogToKeen = false;
            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;
            MyPerServerSettings.GameDSName = MyPerServerSettings.GameNameSafe + "Dedicated";
            MyPerServerSettings.GameDSDescription = "Your place for space engineering, destruction and exploring.";
            MySessionComponentExtDebug.ForceDisable = true;
            MyPerServerSettings.AppId = 244850;
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;

            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
            MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
            MyPlugins.Load();

            MyGlobalTypeMetadata.Static.Init();
            MyInitializer.InvokeBeforeRun(
                MyPerServerSettings.AppId,
                MyPerServerSettings.GameDSName,
                InstancePath, DedicatedServer.AddDateToLog);

        }

        /// <summary>
        /// Start server on the current thread.
        /// </summary>
        public override void Start()
        {
            if (State != ServerState.Stopped)
                throw new InvalidOperationException("Server is already running.");

            Config.Save();
            State = ServerState.Starting;
            Log.Info("Starting server.");

            var runInternal = typeof(DedicatedServer).GetMethod("RunInternal", BindingFlags.Static | BindingFlags.NonPublic);

            MySandboxGame.IsDedicated = true;
            Environment.SetEnvironmentVariable("SteamAppId", MyPerServerSettings.AppId.ToString());

            VRage.Service.ExitListenerSTA.OnExit += delegate { MySandboxGame.Static?.Exit(); };

            do
            {
                runInternal.Invoke(null, null);
            } while (MySandboxGame.IsReloading);

            MyInitializer.InvokeAfterRun();
            State = ServerState.Stopped;
        }

        /// <inheritdoc />
        public override void Init(object gameInstance)
        {
            base.Init(gameInstance);
            State = ServerState.Running;
            SteamServerAPI.Instance.GameServer.SetKeyValue("SM", "Torch");
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            if (State == ServerState.Stopped)
                Log.Error("Server is already stopped");

            if (Thread.CurrentThread.ManagedThreadId != GameThread?.ManagedThreadId && MySandboxGame.Static.IsRunning)
            {
                Invoke(Stop);
                return;
            }

            Log.Info("Stopping server.");
            MySession.Static.Save();
            MySession.Static.Unload();

            //Unload all the static junk.
            //TODO: Finish unloading all server data so it's in a completely clean state.
            MyFileSystem.Reset();
            VRage.Input.MyGuiGameControlsHelpers.Reset();
            VRage.Input.MyInput.UnloadData();

            Log.Info("Server stopped.");
            _stopHandle.Set();
            State = ServerState.Stopped;
        }
    }
}
