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
using System.Threading.Tasks;
using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Engine.Analytics;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using SteamSDK;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Dedicated;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ModAPI;
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
        public InstanceManager DedicatedInstance { get; }
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
        private Stopwatch _uptime;

        public TorchServer(TorchConfig config = null)
        {
            DedicatedInstance = new InstanceManager(this);
            AddManager(DedicatedInstance);
            Config = config ?? new TorchConfig();
            MyFakes.ENABLE_INFINARIO = false;

            var sessionManager = Managers.GetManager<ITorchSessionManager>();
            sessionManager.AddFactory((x) => new MultiplayerManagerDedicated(this));
        }

        /// <inheritdoc />
        public override void Init()
        {
            Log.Info($"Init server '{Config.InstanceName}' at '{Config.InstancePath}'");
            base.Init();

            MyPerGameSettings.SendLogToKeen = false;
            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;
            MyPerServerSettings.GameDSName = MyPerServerSettings.GameNameSafe + "Dedicated";
            MyPerServerSettings.GameDSDescription = "Your place for space engineering, destruction and exploring.";
            MySessionComponentExtDebug.ForceDisable = true;
            MyPerServerSettings.AppId = 244850;
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;
            InvokeBeforeRun();

            //MyObjectBuilderSerializer.RegisterFromAssembly(typeof(MyObjectBuilder_CheckpointSerializer).Assembly);
            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
            MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
            MyPlugins.Load();
            MyGlobalTypeMetadata.Static.Init();

            GetManager<InstanceManager>().LoadInstance(Config.InstancePath);
            Plugins.LoadPlugins();
        }

        private void InvokeBeforeRun()
        {
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

        [ReflectedStaticMethod(Type = typeof(DedicatedServer), Name = "RunInternal")]
        private static Action _dsRunInternal;

        /// <inheritdoc />
        public override void Start()
        {
            if (State != ServerState.Stopped)
                return;

            DedicatedInstance.SaveConfig();
            _uptime = Stopwatch.StartNew();
            IsRunning = true;
            GameThread = Thread.CurrentThread;
            State = ServerState.Starting;
            Log.Info("Starting server.");

            MySandboxGame.IsDedicated = true;
            Environment.SetEnvironmentVariable("SteamAppId", MyPerServerSettings.AppId.ToString());

            VRage.Service.ExitListenerSTA.OnExit += delegate { MySandboxGame.Static?.Exit(); };

            base.Start();
            // Stops RunInternal from calling MyFileSystem.InitUserSpecific(null), we call it in InstanceManager.
            MySandboxGame.IsReloading = true;
            try
            {
                _dsRunInternal.Invoke();
            }
            catch (TargetInvocationException e)
            {
                // Makes log formatting a little nicer.
                throw e.InnerException ?? e;
            }

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
            var elapsed = TimeSpan.FromSeconds(Math.Floor(_uptime.Elapsed.TotalSeconds));
            ElapsedPlayTime = elapsed;

            if (_watchdog == null && Config.TickTimeout > 0)
            {
                Log.Info("Starting server watchdog.");
                _watchdog = new Timer(CheckServerResponding, this, TimeSpan.Zero, TimeSpan.FromSeconds(Config.TickTimeout));
            }
        }

        private static void CheckServerResponding(object state)
        {
            var mre = new ManualResetEvent(false);
            ((TorchServer)state).Invoke(() => mre.Set());
            if (!mre.WaitOne(TimeSpan.FromSeconds(Instance.Config.TickTimeout)))
            {
                var mainThread = MySandboxGame.Static.UpdateThread;
                if (mainThread.IsAlive)
                    mainThread.Suspend();
                var stackTrace = new StackTrace(mainThread, true);
                throw new TimeoutException($"Server watchdog detected that the server was frozen for at least {((TorchServer)state).Config.TickTimeout} seconds.\n{stackTrace}");
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

        /// <summary>
        /// Restart the program. DOES NOT SAVE!
        /// </summary>
        public override void Restart()
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            ((TorchConfig)Config).WaitForPID = Process.GetCurrentProcess().Id.ToString();
            Process.Start(exe, Config.ToString());
            Environment.Exit(0);
        }

        /// <inheritdoc/>
        public override Task Save(long callerId)
        {
            return SaveGameAsync(statusCode => SaveCompleted(statusCode, callerId));
        }

        /// <summary>
        /// Callback for when save has finished.
        /// </summary>
        /// <param name="statusCode">Return code of the save operation</param>
        /// <param name="callerId">Caller of the save operation</param>
        private void SaveCompleted(SaveGameStatus statusCode, long callerId = 0)
        {
            switch (statusCode)
            {
                case SaveGameStatus.Success:
                    Log.Info("Save completed.");
                    // TODO
//                    Multiplayer.SendMessage("Saved game.", playerId: callerId);
                    break;
                case SaveGameStatus.SaveInProgress:
                    Log.Error("Save failed, a save is already in progress.");
//                    Multiplayer.SendMessage("Save failed, a save is already in progress.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                case SaveGameStatus.GameNotReady:
                    Log.Error("Save failed, game was not ready.");
//                    Multiplayer.SendMessage("Save failed, game was not ready.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                case SaveGameStatus.TimedOut:
                    Log.Error("Save failed, save timed out.");
//                    Multiplayer.SendMessage("Save failed, save timed out.", playerId: callerId, font: MyFontEnum.Red);
                    break;
                default:
                    break;
            }
        }
    }
}
