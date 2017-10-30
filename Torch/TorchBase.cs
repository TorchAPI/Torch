using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SpaceEngineers.Game;
using Torch.API;
using Torch.API.Managers;
using Torch.API.ModAPI;
using Torch.API.Session;
using Torch.Commands;
using Torch.Event;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Managers.PatchManager;
using Torch.Managers.Profiler;
using Torch.Patches;
using Torch.Utils;
using Torch.Session;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Common;
using VRage.Game.Components;
using VRage.Game.ObjectBuilder;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;

namespace Torch
{
    /// <summary>
    /// Base class for code shared between the Torch client and server.
    /// </summary>
    public abstract class TorchBase : ViewModel, ITorchBase, IPlugin
    {
        static TorchBase()
        {
            ReflectedManager.Process(typeof(TorchBase).Assembly);
            ReflectedManager.Process(typeof(ITorchBase).Assembly);
            PatchManager.AddPatchShim(typeof(GameStatePatchShim));
            PatchManager.AddPatchShim(typeof(GameAnalyticsPatch));
            PatchManager.AddPatchShim(typeof(KeenLogPatch));
            PatchManager.CommitInternal();
            RegisterCoreAssembly(typeof(ITorchBase).Assembly);
            RegisterCoreAssembly(typeof(TorchBase).Assembly);
            RegisterCoreAssembly(Assembly.GetEntryAssembly());
        }

        /// <summary>
        /// Hack because *keen*.
        /// Use only if necessary, prefer dependency injection.
        /// </summary>
        public static ITorchBase Instance { get; private set; }
        /// <inheritdoc />
        public ITorchConfig Config { get; protected set; }
        /// <inheritdoc />
        public Version TorchVersion { get; }

        /// <summary>
        /// The version of Torch used, with extra data.
        /// </summary>
        public string TorchVersionVerbose { get; }

        /// <inheritdoc />
        public Version GameVersion { get; private set; }
        /// <inheritdoc />
        public string[] RunArgs { get; set; }
        /// <inheritdoc />
        [Obsolete("Use GetManager<T>() or the [Dependency] attribute.")]
        public IPluginManager Plugins { get; protected set; }

        /// <inheritdoc />
        public ITorchSession CurrentSession => Managers?.GetManager<ITorchSessionManager>()?.CurrentSession;

        /// <inheritdoc />
        public event Action SessionLoading;
        /// <inheritdoc />
        public event Action SessionLoaded;
        /// <inheritdoc />
        public event Action SessionUnloading;
        /// <inheritdoc />
        public event Action SessionUnloaded;

        /// <summary>
        /// Common log for the Torch instance.
        /// </summary>
        protected static Logger Log { get; } = LogManager.GetLogger("Torch");

        /// <inheritdoc/>
        public IDependencyManager Managers { get; }

        private bool _init;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a TorchBase instance already exists.</exception>
        protected TorchBase()
        {
            RegisterCoreAssembly(GetType().Assembly);
            if (Instance != null)
                throw new InvalidOperationException("A TorchBase instance already exists.");

            Instance = this;

            TorchVersion = Assembly.GetExecutingAssembly().GetName().Version;
            TorchVersionVerbose = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? TorchVersion.ToString();
            RunArgs = new string[0];

            Managers = new DependencyManager();

            Plugins = new PluginManager(this);

            var sessionManager = new TorchSessionManager(this);
            sessionManager.AddFactory((x) => MyMultiplayer.Static?.SyncLayer != null ? new NetworkManager(this) : null);
            sessionManager.AddFactory((x) => Sync.IsServer ? new ChatManagerServer(this) : new ChatManagerClient(this));
            sessionManager.AddFactory((x) => Sync.IsServer ? new CommandManager(this) : null);
            sessionManager.AddFactory((x) => new EntityManager(this));

            Managers.AddManager(sessionManager);
            Managers.AddManager(new PatchManager(this));
            Managers.AddManager(new FilesystemManager(this));
            Managers.AddManager(new UpdateManager(this));
            Managers.AddManager(new EventManager(this));
            Managers.AddManager(new ProfilerManager(this));
            Managers.AddManager(Plugins);
            TorchAPI.Instance = this;
        }

        [Obsolete("Prefer using Managers.GetManager for global managers")]
        public T GetManager<T>() where T : class, IManager
        {
            return Managers.GetManager<T>();
        }

        [Obsolete("Prefer using Managers.AddManager for global managers")]
        public bool AddManager<T>(T manager) where T : class, IManager
        {
            return Managers.AddManager(manager);
        }

        public bool IsOnGameThread()
        {
            return Thread.CurrentThread.ManagedThreadId == MySandboxGame.Static.UpdateThread.ManagedThreadId;
        }

        public Task SaveGameAsync(Action<SaveGameStatus> callback)
        {
            Log.Info("Saving game");

            if (!MySandboxGame.IsGameReady)
            {
                callback?.Invoke(SaveGameStatus.GameNotReady);
            }
            else if (MyAsyncSaving.InProgress)
            {
                callback?.Invoke(SaveGameStatus.SaveInProgress);
            }
            else
            {
                var e = new AutoResetEvent(false);
                MyAsyncSaving.Start(() => e.Set());

                return Task.Run(() =>
                {
                    callback?.Invoke(e.WaitOne(5000) ? SaveGameStatus.Success : SaveGameStatus.TimedOut);
                    e.Dispose();
                });
            }

            return Task.CompletedTask;
        }

        #region Game Actions

        /// <summary>
        /// Invokes an action on the game thread.
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action)
        {
            MySandboxGame.Static.Invoke(action);
        }

        /// <summary>
        /// Invokes an action on the game thread asynchronously.
        /// </summary>
        /// <param name="action"></param>
        public Task InvokeAsync(Action action)
        {
            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(InvokeAsync)} should not be called on the game thread.");
                action?.Invoke();
                return Task.CompletedTask;
            }

            return Task.Run(() => InvokeBlocking(action));
        }

        /// <summary>
        /// Invokes an action on the game thread and blocks until it is completed.
        /// </summary>
        /// <param name="action"></param>
        public void InvokeBlocking(Action action)
        {
            if (action == null)
                return;

            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(InvokeBlocking)} should not be called on the game thread.");
                action.Invoke();
                return;
            }

            var e = new AutoResetEvent(false);

            MySandboxGame.Static.Invoke(() =>
            {
                try
                {
                    action.Invoke();
                }
                finally
                {
                    e.Set();
                }
            });

            if (!e.WaitOne(60000))
                throw new TimeoutException("The game action timed out.");
        }

        #endregion

        /// <inheritdoc />
        public virtual void Init()
        {
            Debug.Assert(!_init, "Torch instance is already initialized.");
            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();
            RegisterFromCallingAssemblyPatch.ForceRegisterAssemblies();

            Debug.Assert(MyPerGameSettings.BasicGameInfo.GameVersion != null, "MyPerGameSettings.BasicGameInfo.GameVersion != null");
            GameVersion = new Version(new MyVersion(MyPerGameSettings.BasicGameInfo.GameVersion.Value).FormattedText.ToString().Replace("_", "."));
            try
            {
                Console.Title = $"{Config.InstanceName} - Torch {TorchVersion}, SE {GameVersion}";
            }
            catch
            {
                // Running as service
            }

#if DEBUG
            Log.Info("DEBUG");
#else
            Log.Info("RELEASE");
#endif
            Log.Info($"Torch Version: {TorchVersionVerbose}");
            Log.Info($"Game Version: {GameVersion}");
            Log.Info($"Executing assembly: {Assembly.GetEntryAssembly().FullName}");
            Log.Info($"Executing directory: {AppDomain.CurrentDomain.BaseDirectory}");

            MySession.OnLoading += OnSessionLoading;
            MySession.AfterLoading += OnSessionLoaded;
            MySession.OnUnloading += OnSessionUnloading;
            MySession.OnUnloaded += OnSessionUnloaded;
            RegisterVRagePlugin();
            Managers.GetManager<PluginManager>().LoadPlugins();
            Managers.Attach();
            _init = true;
        }

        private void OnSessionLoading()
        {
            Log.Debug("Session loading");
            try
            {
                SessionLoading?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnSessionLoaded()
        {
            Log.Debug("Session loaded");
            try
            {
                SessionLoaded?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnSessionUnloading()
        {
            Log.Debug("Session unloading");
            try
            {
                SessionUnloading?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnSessionUnloaded()
        {
            Log.Debug("Session unloaded");
            try
            {
                SessionUnloaded?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Hook into the VRage plugin system for updates.
        /// </summary>
        private void RegisterVRagePlugin()
        {
            var fieldName = "m_plugins";
            var pluginList = typeof(MyPlugins).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as List<IPlugin>;
            if (pluginList == null)
                throw new TypeLoadException($"{fieldName} field not found in {nameof(MyPlugins)}");

            pluginList.Add(this);
        }

        /// <inheritdoc/>
        public virtual Task Save(long callerId)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/> 
        public virtual void Start()
        {

        }

        /// <inheritdoc />
        public virtual void Stop()
        {

        }

        /// <inheritdoc />
        public virtual void Restart()
        {

        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Managers.Detach();
        }

        /// <inheritdoc />
        public virtual void Init(object gameInstance)
        {

        }

        /// <inheritdoc />
        public virtual void Update()
        {
            Managers.GetManager<IPluginManager>().UpdatePlugins();
        }


        private TorchGameState _gameState = TorchGameState.Unloaded;

        /// <inheritdoc/>
        public TorchGameState GameState
        {
            get => _gameState;
            internal set
            {
                _gameState = value;
                GameStateChanged?.Invoke(MySandboxGame.Static, _gameState);
            }
        }

        /// <inheritdoc/>
        public event TorchGameStateChangedDel GameStateChanged;

        private static readonly HashSet<Assembly> _registeredCoreAssemblies = new HashSet<Assembly>();
        /// <summary>
        /// Registers a core (Torch) assembly with the system, including its
        /// <see cref="EventManager"/> shims, <see cref="PatchManager"/> shims, and <see cref="ReflectedManager"/> components.
        /// </summary>
        /// <param name="asm">Assembly to register</param>
        internal static void RegisterCoreAssembly(Assembly asm)
        {
            lock (_registeredCoreAssemblies)
                if (_registeredCoreAssemblies.Add(asm))
                {
                    ReflectedManager.Process(asm);
                    EventManager.AddDispatchShims(asm);
                    PatchManager.AddPatchShims(asm);
                }
        }

        private static readonly HashSet<Assembly> _registeredAuxAssemblies = new HashSet<Assembly>();

        /// <summary>
        /// Registers an auxillary (plugin) assembly with the system, including its
        /// <see cref="ReflectedManager"/> related components.
        /// </summary>
        /// <param name="asm">Assembly to register</param>
        internal static void RegisterAuxAssembly(Assembly asm)
        {
            lock (_registeredAuxAssemblies)
                if (_registeredAuxAssemblies.Add(asm))
                {
                    ReflectedManager.Process(asm);
                }
        }
    }
}
