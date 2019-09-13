#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Commands;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Server.Commands;
using Torch.Server.Managers;
using Torch.Utils;
using VRage;
using VRage.Dedicated;
using VRage.Dedicated.RemoteAPI;
using VRage.GameServices;
using VRage.Scripting;
using VRage.Steam;
using Timer = System.Threading.Timer;

#endregion

#pragma warning disable 618

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        private bool _canRun;
        private TimeSpan _elapsedPlayTime;
        private bool _hasRun;
        private bool _isRunning;
        private float _simRatio;
        private ServerState _state;
        private Stopwatch _uptime;
        private Timer _watchdog;
        private int _players;
        private MultiplayerManagerDedicated _multiplayerManagerDedicated;
        
        internal bool FatalException { get; set; }

        /// <inheritdoc />
       public TorchServer(TorchConfig config = null)
        {
            DedicatedInstance = new InstanceManager(this);
            AddManager(DedicatedInstance);
            AddManager(new EntityControlManager(this));
            AddManager(new RemoteAPIManager(this));
            Config = config ?? new TorchConfig();

            var sessionManager = Managers.GetManager<ITorchSessionManager>();
            sessionManager.AddFactory(x => new MultiplayerManagerDedicated(this));
        }

        /// <inheritdoc />
        public float SimulationRatio { get => _simRatio; set => SetValue(ref _simRatio, value); }

        /// <inheritdoc />
        public TimeSpan ElapsedPlayTime { get => _elapsedPlayTime; set => SetValue(ref _elapsedPlayTime, value); }

        /// <inheritdoc />
        public Thread GameThread { get; private set; }

        /// <inheritdoc />
        public bool IsRunning { get => _isRunning; set => SetValue(ref _isRunning, value); }

        public bool CanRun { get => _canRun; set => SetValue(ref _canRun, value); }

        /// <inheritdoc />
        public InstanceManager DedicatedInstance { get; }

        /// <inheritdoc />
        public string InstanceName => Config?.InstanceName;

        /// <inheritdoc />
        protected override uint SteamAppId => 244850;

        /// <inheritdoc />
        protected override string SteamAppName => "SpaceEngineersDedicated";

        /// <inheritdoc />
        public ServerState State { get => _state; private set => SetValue(ref _state, value); }

        public event Action<ITorchServer> Initialized;

        /// <inheritdoc />
        public string InstancePath => Config?.InstancePath;

        public int OnlinePlayers { get => _players; private set => SetValue(ref _players, value); }

        /// <inheritdoc />
        public override void Init()
        {
            Log.Info("Initializing server");
            MySandboxGame.IsDedicated = true;
            base.Init();
            Managers.GetManager<ITorchSessionManager>().SessionStateChanged += OnSessionStateChanged;
            GetManager<InstanceManager>().LoadInstance(Config.InstancePath);
            CanRun = true;
            Initialized?.Invoke(this);
            Log.Info($"Initialized server '{Config.InstanceName}' at '{Config.InstancePath}'");
        }

        /// <inheritdoc />
        public override void Start()
        {
            if (State != ServerState.Stopped)
                return;

            if (_hasRun)
            {
                Restart();
                return;
            }

            State = ServerState.Starting;
            IsRunning = true;
            CanRun = false;
            _hasRun = true;
            Log.Info("Starting server.");
            MySandboxGame.ConfigDedicated = DedicatedInstance.DedicatedConfig.Model;

            _uptime = Stopwatch.StartNew();
            base.Start();
        }

        /// <inheritdoc />
        public override void Stop()
        {
            if (State == ServerState.Stopped)
                Log.Error("Server is already stopped");
            Log.Info("Stopping server.");
            base.Stop();
            Log.Info("Server stopped.");

            State = ServerState.Stopped;
            IsRunning = false;
            CanRun = true;
        }

        /// <summary>
        ///     Restart the program.
        /// </summary>
        public override void Restart(bool save = true)
        {
            if (Config.DisconnectOnRestart)
            {
                ModCommunication.SendMessageToClients(new JoinServerMessage("0.0.0.0:25555"));
                Log.Info("Ejected all players from server for restart.");
            }

            if (IsRunning && save)
                Save().ContinueWith(DoRestart, this, TaskContinuationOptions.RunContinuationsAsynchronously);
            else
                DoRestart(null, this);

            void DoRestart(Task<GameSaveResult> task, object torch0)
            {
                var torch = (TorchServer)torch0;
                torch.Stop();
                // TODO clone this
                var config = (TorchConfig)torch.Config;
                LogManager.Flush();

                string exe = Assembly.GetExecutingAssembly().Location;
                Debug.Assert(exe != null);
                config.WaitForPID = Process.GetCurrentProcess().Id.ToString();
                config.Autostart = true;
                Process.Start(exe, config.ToString());

                Process.GetCurrentProcess().Kill();
            }
        }

        private void OnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Unloading || newState == TorchSessionState.Unloaded)
            {
                _watchdog?.Dispose();
                _watchdog = null;
                ModCommunication.Unregister();
            }

            if (newState == TorchSessionState.Loaded)
            {
                _multiplayerManagerDedicated = CurrentSession.Managers.GetManager<MultiplayerManagerDedicated>();
                CurrentSession.Managers.GetManager<CommandManager>().RegisterCommandModule(typeof(WhitelistCommands));
                ModCommunication.Register();
            }
        }

        /// <inheritdoc />
        public override void Init(object gameInstance)
        {
            base.Init(gameInstance);
            if (gameInstance is MySandboxGame && MySession.Static != null)
                State = ServerState.Running;
            else
                State = ServerState.Stopped;
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();
            // Stops 1.00-1.02 flicker.
            SimulationRatio = Math.Min(Sync.ServerSimulationRatio, 1);
            var elapsed = TimeSpan.FromSeconds(Math.Floor(_uptime.Elapsed.TotalSeconds));
            ElapsedPlayTime = elapsed;
            OnlinePlayers = _multiplayerManagerDedicated?.Players.Count ?? 0;

            if (_watchdog == null && Config.TickTimeout > 0)
            {
                Log.Info("Starting server watchdog.");
                _watchdog = new Timer(CheckServerResponding, this, TimeSpan.Zero,
                    TimeSpan.FromSeconds(Config.TickTimeout));
            }
        }

        #region Freeze Detection

        private static void CheckServerResponding(object state)
        {
            var server = (TorchServer)state;
            var mre = new ManualResetEvent(false);
            server.Invoke(() => mre.Set());
            if (!mre.WaitOne(TimeSpan.FromSeconds(Instance.Config.TickTimeout)))
            {
                if (server.FatalException)
                {
                    server._watchdog.Dispose();
                    return;
                }
#if DEBUG
                Log.Error(
                    $"Server watchdog detected that the server was frozen for at least {((TorchServer) state).Config.TickTimeout} seconds.");
                Log.Error(DumpFrozenThread(MySandboxGame.Static.UpdateThread));
#else
                Log.Error(DumpFrozenThread(MySandboxGame.Static.UpdateThread));
                throw new TimeoutException($"Server watchdog detected that the server was frozen for at least {((TorchServer)state).Config.TickTimeout} seconds.");
#endif
            }

            Log.Debug("Server watchdog responded");
        }

        private static string DumpFrozenThread(Thread thread, int traces = 3, int pause = 5000)
        {
            var stacks = new List<string>(traces);
            var totalSize = 0;
            for (var i = 0; i < traces; i++)
            {
                string dump = DumpStack(thread).ToString();
                totalSize += dump.Length;
                stacks.Add(dump);
                Thread.Sleep(pause);
            }

            string commonPrefix = StringUtils.CommonSuffix(stacks);
            // Advance prefix to include the line terminator.
            commonPrefix = commonPrefix.Substring(commonPrefix.IndexOf('\n') + 1);

            var result = new StringBuilder(totalSize - (stacks.Count - 1) * commonPrefix.Length);
            result.AppendLine($"Frozen thread dump {thread.Name}");
            result.AppendLine("Common prefix:").AppendLine(commonPrefix);
            for (var i = 0; i < stacks.Count; i++)
                if (stacks[i].Length > commonPrefix.Length)
                {
                    result.AppendLine($"Suffix {i}");
                    result.AppendLine(stacks[i].Substring(0, stacks[i].Length - commonPrefix.Length));
                }

            return result.ToString();
        }

        private static StackTrace DumpStack(Thread thread)
        {
            try
            {
                thread.Suspend();
            }
            catch
            {
                // ignored
            }

            var stack = new StackTrace(thread, true);
            try
            {
                thread.Resume();
            }
            catch
            {
                // ignored
            }

            return stack;
        }

        #endregion
    }
}