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
using System.Threading;
using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Engine.Analytics;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using SteamSDK;
using Torch.API;
using Torch.Managers;
using VRage.Dedicated;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilder;
using VRage.Game.SessionComponents;
using VRage.Library;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;
#pragma warning disable 618

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        //public MyConfigDedicated<MyObjectBuilder_SessionSettings> DedicatedConfig { get; set; }
        public float SimulationRatio { get => _simRatio; set { _simRatio = value; OnPropertyChanged(); } }
        public TimeSpan ElapsedPlayTime { get => _elapsedPlayTime; set { _elapsedPlayTime = value; OnPropertyChanged(); } }
        public Thread GameThread { get; private set; }
        public ServerState State { get => _state; private set { _state = value; OnPropertyChanged(); } }
        public bool IsRunning { get => _isRunning; set { _isRunning = value; OnPropertyChanged(); } }
        /// <inheritdoc />
        public string InstanceName => Config?.InstanceName;
        /// <inheritdoc />
        public string InstancePath => Config?.InstancePath;

        private bool _isRunning;
        private ServerState _state;
        private TimeSpan _elapsedPlayTime;
        private float _simRatio;
        private readonly AutoResetEvent _stopHandle = new AutoResetEvent(false);
        private Timer _watchdog;

        public TorchServer(TorchConfig config = null)
        {
            Config = config ?? new TorchConfig();
            MyFakes.ENABLE_INFINARIO = false;
        }

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            Log.Info($"Init server '{Config.InstanceName}' at '{Config.InstancePath}'");

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

        private void InvokeBeforeRun()
        {
            
            var contentPath = "Content";

            var privateContentPath = typeof(MyFileSystem).GetField("m_contentPath", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string;
            if (privateContentPath != null)
                Log.Debug("MyFileSystem already initialized");
            else
            {
                MyFileSystem.ExePath = Path.Combine(GetManager<FilesystemManager>().TorchDirectory, "DedicatedServer64");
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
            MySandboxGame.Log.WriteLine("IntPtr.Size: " + IntPtr.Size);
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

        /// <inheritdoc />
        public override void Start()
        {
            if (State != ServerState.Stopped)
                return;

            IsRunning = true;
            GameThread = Thread.CurrentThread;
            Config.Save();
            State = ServerState.Starting;
            Log.Info("Starting server.");

            var runInternal = typeof(DedicatedServer).GetMethod("RunInternal", BindingFlags.Static | BindingFlags.NonPublic);

            MySandboxGame.IsDedicated = true;
            Environment.SetEnvironmentVariable("SteamAppId", MyPerServerSettings.AppId.ToString());

            VRage.Service.ExitListenerSTA.OnExit += delegate { MySandboxGame.Static?.Exit(); };

            base.Start();
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

            if (_watchdog == null && Instance.Config.TickTimeout > 0)
            {
                Log.Info("Starting server watchdog.");
                _watchdog = new Timer(CheckServerResponding, this, TimeSpan.Zero, TimeSpan.FromSeconds(Instance.Config.TickTimeout));
            }
        }

        private static void CheckServerResponding(object state)
        {
            var mre = new ManualResetEvent(false);
            ((TorchServer)state).Invoke(() => mre.Set());
            if (!mre.WaitOne(TimeSpan.FromSeconds(Instance.Config.TickTimeout)))
            {
                var mainThread = MySandboxGame.Static.UpdateThread;
                mainThread.Suspend();
                var stackTrace = new StackTrace(mainThread, true);
                throw new TimeoutException($"Server watchdog detected that the server was frozen for at least {Instance.Config.TickTimeout} seconds.\n{stackTrace}");
            }

            Log.Debug("Server watchdog responded");
        }

        /// <inheritdoc />
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
            IsRunning = false;
        }

        public void Restart()
        {
            
        }

        /// <inheritdoc/>
        public override void Save(long callerId)
        {
            base.SaveGameAsync((statusCode) => SaveCompleted(statusCode, callerId));
        }

        /// <summary>
        /// Callback for when save has finished.
        /// </summary>
        /// <param name="statusCode">Return code of the save operation</param>
        /// <param name="callerId">Caller of the save operation</param>
        private void SaveCompleted(SaveGameStatus statusCode, long callerId)
        {
            switch (statusCode)
            {
                case SaveGameStatus.Success:
                    Log.Info("Save completed.");
                    Multiplayer.SendMessage("Saved game.", playerId: callerId);
                    break;
                case SaveGameStatus.SaveInProgress:
                    Log.Error("Save failed, a save is already in progress.");
                    Multiplayer.SendMessage("Save failed, a save is already in progress.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                case SaveGameStatus.GameNotReady:
                    Log.Error("Save failed, game was not ready.");
                    Multiplayer.SendMessage("Save failed, game was not ready.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                case SaveGameStatus.TimedOut:
                    Log.Error("Save failed, save timed out.");
                    Multiplayer.SendMessage("Save failed, save timed out.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                default:
                    break;
            }
        }
    }
}
