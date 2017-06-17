using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Havok;
using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using Torch.API;
using VRage.Dedicated;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilder;
using VRage.Game.SessionComponents;
using VRage.Library;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Trace;
using VRage.Utils;

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        //public MyConfigDedicated<MyObjectBuilder_SessionSettings> DedicatedConfig { get; set; }
        public float SimulationRatio { get => _simRatio; set { _simRatio = value; OnPropertyChanged(); } }
        public TimeSpan ElapsedPlayTime { get => _elapsedPlayTime; set { _elapsedPlayTime = value; OnPropertyChanged(); } }
        public Thread GameThread { get; private set; }
        public ServerState State { get => _state; private set { _state = value; OnPropertyChanged(); } }
        public string InstanceName => Config?.InstanceName;
        public string InstancePath => Config?.InstancePath;

        private ServerState _state;
        private TimeSpan _elapsedPlayTime;
        private float _simRatio;
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

            MyObjectBuilderSerializer.RegisterFromAssembly(typeof(MyObjectBuilder_CheckpointSerializer).Assembly);
            
            InvokeBeforeRun();

            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
            MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
            MyPlugins.Load();
            MyGlobalTypeMetadata.Static.Init();
            RuntimeHelpers.RunClassConstructor(typeof(MyObjectBuilder_Base).TypeHandle);
        }

        public void InvokeBeforeRun()
        {
            
            var contentPath = "Content";

            var privateContentPath = typeof(MyFileSystem).GetField("m_contentPath", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
            if (privateContentPath != null)
                Log.Debug("MyFileSystem already initialized");
            else
            {
                if (Program.IsManualInstall)
                {
                    var rootPath = new FileInfo(MyFileSystem.ExePath).Directory.FullName;
                    contentPath = Path.Combine(rootPath, "Content");
                }
                else
                {
                    MyFileSystem.ExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DedicatedServer64");
                }

                MyFileSystem.Init(contentPath, InstancePath);
            }

            MySandboxGame.Log.Init("SpaceEngineers-Dedicated.log", MyFinalBuildConstants.APP_VERSION_STRING);
            MySandboxGame.Log.WriteLine("Steam build: Always true");
            MySandboxGame.Log.WriteLine("Environment.ProcessorCount: " + MyEnvironment.ProcessorCount);
            //MySandboxGame.Log.WriteLine("Environment.OSVersion: " + GetOsName());
            MySandboxGame.Log.WriteLine("Environment.CommandLine: " + Environment.CommandLine);
            MySandboxGame.Log.WriteLine("Environment.Is64BitProcess: " + MyEnvironment.Is64BitProcess);
            MySandboxGame.Log.WriteLine("Environment.Is64BitOperatingSystem: " + Environment.Is64BitOperatingSystem);
            //MySandboxGame.Log.WriteLine("Environment.Version: " + GetNETFromRegistry());
            MySandboxGame.Log.WriteLine("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
            MySandboxGame.Log.WriteLine("MainAssembly.ProcessorArchitecture: " + Assembly.GetExecutingAssembly().GetArchitecture());
            MySandboxGame.Log.WriteLine("ExecutingAssembly.ProcessorArchitecture: " + MyFileSystem.MainAssembly.GetArchitecture());
            MySandboxGame.Log.WriteLine("IntPtr.Size: " + IntPtr.Size.ToString());
            MySandboxGame.Log.WriteLine("Default Culture: " + CultureInfo.CurrentCulture.Name);
            MySandboxGame.Log.WriteLine("Default UI Culture: " + CultureInfo.CurrentUICulture.Name);
            MySandboxGame.Log.WriteLine("IsAdmin: " + new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

            MyLog.Default = MySandboxGame.Log;

            Thread.CurrentThread.Name = "Main thread";

            //Because we want exceptions from users to be in english
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            MySandboxGame.Config = new MyConfig("SpaceEngineers-Dedicated.cfg");
            MySandboxGame.Config.Load();
        }

        /// <summary>
        /// Start server on the current thread.
        /// </summary>
        public override void Start()
        {
            if (State != ServerState.Stopped)
                return;

            GameThread = Thread.CurrentThread;
            Config.Save();
            State = ServerState.Starting;
            Log.Info("Starting server.");

            var runInternal = typeof(DedicatedServer).GetMethod("RunInternal", BindingFlags.Static | BindingFlags.NonPublic);

            MySandboxGame.IsDedicated = true;
            Environment.SetEnvironmentVariable("SteamAppId", MyPerServerSettings.AppId.ToString());

            VRage.Service.ExitListenerSTA.OnExit += delegate { MySandboxGame.Static?.Exit(); };

            runInternal.Invoke(null, null);

            MySandboxGame.Log.Close();
            State = ServerState.Stopped;
        }

        /// <inheritdoc />
        public override void Init(object gameInstance)
        {
            base.Init(gameInstance);
            State = ServerState.Running;
            SteamServerAPI.Instance.GameServer.SetKeyValue("SM", "Torch");
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();
            SimulationRatio = Sync.ServerSimulationRatio;
            ElapsedPlayTime = MySession.Static?.ElapsedPlayTime ?? default(TimeSpan);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            if (State == ServerState.Stopped)
                Log.Error("Server is already stopped");

            if (Thread.CurrentThread != MySandboxGame.Static.UpdateThread)
            {
                Log.Debug("Invoking server stop on game thread.");
                Invoke(Stop);
                return;
            }

            Log.Info("Stopping server.");

            //Unload all the static junk.
            //TODO: Finish unloading all server data so it's in a completely clean state.
            MySandboxGame.Static.Exit();

            Log.Info("Server stopped.");
            _stopHandle.Set();
            State = ServerState.Stopped;
        }
    }
}
