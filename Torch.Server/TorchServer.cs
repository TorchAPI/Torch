using NLog;
using Sandbox;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Game;

#pragma warning disable 618

namespace Torch.Server
{
    public class TorchServer : TorchBase, ITorchServer
    {
        //public MyConfigDedicated<MyObjectBuilder_SessionSettings> DedicatedConfig { get; set; }
        /// <inheritdoc />
        public float SimulationRatio
        {
            get => _simRatio;
            set
            {
                _simRatio = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public TimeSpan ElapsedPlayTime
        {
            get => _elapsedPlayTime;
            set
            {
                _elapsedPlayTime = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public Thread GameThread { get; private set; }

        /// <inheritdoc />
        public ServerState State
        {
            get => _state;
            private set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public InstanceManager DedicatedInstance { get; }

        /// <inheritdoc />
        public string InstanceName => Config?.InstanceName;

        /// <inheritdoc />
        public string InstancePath => Config?.InstancePath;

        private bool _isRunning;
        private ServerState _state;
        private TimeSpan _elapsedPlayTime;
        private float _simRatio;
        private Timer _watchdog;
        private Stopwatch _uptime;

        /// <inheritdoc />
        public TorchServer(TorchConfig config = null)
        {
            DedicatedInstance = new InstanceManager(this);
            AddManager(DedicatedInstance);
            AddManager(new EntityControlManager(this));
            Config = config ?? new TorchConfig();

            var sessionManager = Managers.GetManager<ITorchSessionManager>();
            sessionManager.AddFactory((x) => new MultiplayerManagerDedicated(this));
        }

        /// <inheritdoc/>
        protected override uint SteamAppId => 244850;

        /// <inheritdoc/>
        protected override string SteamAppName => "SpaceEngineersDedicated";

        /// <inheritdoc />
        public override void Init()
        {
            Log.Info($"Init server '{Config.InstanceName}' at '{Config.InstancePath}'");
            Sandbox.Engine.Platform.Game.IsDedicated = true;

            base.Init();
            Managers.GetManager<ITorchSessionManager>().SessionStateChanged += OnSessionStateChanged;
            GetManager<InstanceManager>().LoadInstance(Config.InstancePath);
        }

        private void OnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Unloading || newState == TorchSessionState.Unloaded)
            {
                _watchdog?.Dispose();
                _watchdog = null;
            }
        }

        /// <inheritdoc />
        public override void Start()
        {
            if (State != ServerState.Stopped)
                return;
            State = ServerState.Starting;
            IsRunning = true;
            Log.Info("Starting server.");
            MySandboxGame.ConfigDedicated = DedicatedInstance.DedicatedConfig.Model;

            DedicatedInstance.SaveConfig();
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
        }

        /// <summary>
        /// Restart the program.
        /// </summary>
        public override void Restart()
        {
            Save().ContinueWith((task, torchO) =>
            {
                var torch = (TorchServer) torchO;
                torch.Stop();
                // TODO clone this
                var config = (TorchConfig) torch.Config;
                LogManager.Flush();

                string exe = Assembly.GetExecutingAssembly().Location;
                Debug.Assert(exe != null);
                config.WaitForPID = Process.GetCurrentProcess().Id.ToString();
                config.Autostart = true;
                Process.Start(exe, config.ToString());

                Process.GetCurrentProcess().Kill();
            }, this, TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        /// <inheritdoc />
        public override void Init(object gameInstance)
        {
            base.Init(gameInstance);
            var game = gameInstance as MySandboxGame;
            if (game != null && MySession.Static != null)
            {
                State = ServerState.Running;
//                SteamServerAPI.Instance.GameServer.SetKeyValue("SM", "Torch");
            }
            else
            {
                State = ServerState.Stopped;
            }
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
                _watchdog = new Timer(CheckServerResponding, this, TimeSpan.Zero,
                    TimeSpan.FromSeconds(Config.TickTimeout));
            }
        }

        #region Freeze Detection

        private static void CheckServerResponding(object state)
        {
            var mre = new ManualResetEvent(false);
            ((TorchServer) state).Invoke(() => mre.Set());
            if (!mre.WaitOne(TimeSpan.FromSeconds(Instance.Config.TickTimeout)))
            {
#if DEBUG
                Log.Error(
                    $"Server watchdog detected that the server was frozen for at least {((TorchServer) state).Config.TickTimeout} seconds.");
                Log.Error(DumpFrozenThread(MySandboxGame.Static.UpdateThread));
#else
                Log.Error(DumpFrozenThread(MySandboxGame.Static.UpdateThread));
                throw new TimeoutException($"Server watchdog detected that the server was frozen for at least {((TorchServer)state).Config.TickTimeout} seconds.");
#endif
            }
            else
            {
                Log.Debug("Server watchdog responded");
            }
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