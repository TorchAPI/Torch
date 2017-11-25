using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ProtoBuf.Meta;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using Torch.API;
using Torch.API.Managers;
using Torch.API.ModAPI;
using Torch.API.Session;
using Torch.Commands;
using Torch.Event;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Managers.PatchManager;
using Torch.Patches;
using Torch.Utils;
using Torch.Session;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Common;
using VRage.Game.Components;
using VRage.Game.ObjectBuilder;
using VRage.Game.SessionComponents;
using VRage.GameServices;
using VRage.Library;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Steam;
using VRage.Utils;
using VRageRender;

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
            TorchVersionVerbose = Assembly.GetEntryAssembly()
                                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                      ?.InformationalVersion ?? TorchVersion.ToString();
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
            Managers.AddManager(Plugins);
            TorchAPI.Instance = this;

            GameStateChanged += (game, state) =>
            {
                if (state == TorchGameState.Created)
                {
                    // If the attached assemblies change (MySandboxGame.ctor => MySandboxGame.ParseArgs => MyPlugins.RegisterFromArgs)
                    // attach assemblies to object factories again.
                    ObjectFactoryInitPatch.ForceRegisterAssemblies();
                    // safe to commit here; all important static ctors have run
                    PatchManager.CommitInternal();
                }
            };

            sessionManager.SessionStateChanged += (session, state) =>
            {
                switch (state)
                {
                    case TorchSessionState.Loading:
                        SessionLoading?.Invoke();
                        break;
                    case TorchSessionState.Loaded:
                        SessionLoaded?.Invoke();
                        break;
                    case TorchSessionState.Unloading:
                        SessionUnloading?.Invoke();
                        break;
                    case TorchSessionState.Unloaded:
                        SessionUnloaded?.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            };
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Invoke(Action action, [CallerMemberName] string caller = "")
        {
            MySandboxGame.Static.Invoke(action, caller);
        }

        /// <summary>
        /// Invokes an action on the game thread asynchronously.
        /// </summary>
        /// <param name="action"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Task InvokeAsync(Action action, [CallerMemberName] string caller = "")
        {
            if (Thread.CurrentThread == MySandboxGame.Static.UpdateThread)
            {
                Debug.Assert(false, $"{nameof(InvokeAsync)} should not be called on the game thread.");
                action?.Invoke();
                return Task.CompletedTask;
            }

            return Task.Run(() => InvokeBlocking(action, caller));
        }

        /// <summary>
        /// Invokes an action on the game thread and blocks until it is completed.
        /// </summary>
        /// <param name="action"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InvokeBlocking(Action action, [CallerMemberName] string caller = "")
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
            }, caller);

            if (!e.WaitOne(60000))
                throw new TimeoutException("The game action timed out.");
        }

        #endregion

        #region Torch Init/Destroy

        protected abstract uint SteamAppId { get; }
        protected abstract string SteamAppName { get; }

        /// <inheritdoc />
        public virtual void Init()
        {
            Debug.Assert(!_init, "Torch instance is already initialized.");
            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();
            ObjectFactoryInitPatch.ForceRegisterAssemblies();

            Debug.Assert(MyPerGameSettings.BasicGameInfo.GameVersion != null,
                "MyPerGameSettings.BasicGameInfo.GameVersion != null");
            GameVersion = new Version(new MyVersion(MyPerGameSettings.BasicGameInfo.GameVersion.Value).FormattedText
                .ToString().Replace("_", "."));
            try
            {
                Console.Title = $"{Config.InstanceName} - Torch {TorchVersion}, SE {GameVersion}";
            }
            catch
            {
                // Running without a console
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

            InitVRageInstance();
            Managers.GetManager<PluginManager>().LoadPlugins();
            Managers.Attach();
            _init = true;

            if (GameState >= TorchGameState.Created && GameState < TorchGameState.Unloading)
                // safe to commit here; all important static ctors have run
                PatchManager.CommitInternal();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Managers.Detach();
            DisposeVRageInstance();
        }

        #endregion

        #region VRage Instance Init/Destroy

#pragma warning disable 649
        [ReflectedGetter(Name = "m_plugins", Type = typeof(MyPlugins))]
        private static readonly Func<List<IPlugin>> _getVRagePluginList;
#pragma warning restore 649

        protected SpaceEngineersGame GameInstance { get; private set; }

        /// <summary>
        /// Sets up the VRage instance.
        /// Any flags (ie <see cref="Sandbox.Engine.Platform.Game.IsDedicated"/>) must be set before this is called.
        /// </summary>
        protected virtual void InitVRageInstance()
        {
            bool dedicated = Sandbox.Engine.Platform.Game.IsDedicated;
            Environment.SetEnvironmentVariable("SteamAppId", SteamAppId.ToString());
            MyServiceManager.Instance.AddService<IMyGameService>(new MySteamService(dedicated, SteamAppId));
            if (dedicated && !MyGameService.HasGameServer)
            {
                Log.Warn("Steam service is not running! Please reinstall dedicated server.");
                return;
            }

            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;
            MySessionComponentExtDebug.ForceDisable = true;
            MyPerGameSettings.SendLogToKeen = false;
            // SpaceEngineersGame.SetupAnalytics();

            MyFileSystem.ExePath = Path.GetDirectoryName(typeof(SpaceEngineersGame).Assembly.Location);

            TweakGameSettings();

            MyInitializer.InvokeBeforeRun(SteamAppId, SteamAppName, Config.InstancePath);
            // MyInitializer.InitCheckSum();


            // Hook into the VRage plugin system for updates.
            _getVRagePluginList().Add(this);

            if (!MySandboxGame.IsReloading)
                MyFileSystem.InitUserSpecific(dedicated ? null : MyGameService.UserId.ToString());
            MySandboxGame.IsReloading = dedicated;

            // render init
            {
                IMyRender renderer = null;
                if (dedicated)
                {
                    renderer = new MyNullRender();
                }
                else
                {
                    MyPerformanceSettings preset = MyGuiScreenOptionsGraphics.GetPreset(MyRenderQualityEnum.NORMAL);
                    MyRenderProxy.Settings.User = MyVideoSettingsManager.GetGraphicsSettingsFromConfig(ref preset)
                        .PerformanceSettings.RenderSettings;
                    MyStringId graphicsRenderer = MySandboxGame.Config.GraphicsRenderer;
                    if (graphicsRenderer == MySandboxGame.DirectX11RendererKey)
                    {
                        renderer = new MyDX11Render(new MyRenderSettings?(MyRenderProxy.Settings));
                        if (!renderer.IsSupported)
                        {
                            MySandboxGame.Log.WriteLine(
                                "DirectX 11 renderer not supported. No renderer to revert back to.");
                            renderer = null;
                        }
                    }
                    if (renderer == null)
                    {
                        throw new MyRenderException(
                            "The current version of the game requires a Dx11 card. \\n For more information please see : http://blog.marekrosa.org/2016/02/space-engineers-news-full-source-code_26.html",
                            MyRenderExceptionEnum.GpuNotSupported);
                    }
                    MySandboxGame.Config.GraphicsRenderer = graphicsRenderer;
                }
                MyRenderProxy.Initialize(renderer);
                MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
                MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
            }

            GameInstance = new SpaceEngineersGame(RunArgs);
        }

        /// <summary>
        /// Called after the basic game information is filled, but before the game is created.
        /// </summary>
        protected virtual void TweakGameSettings()
        {
        }

        /// <summary>
        /// Tears down the VRage instance
        /// </summary>
        protected virtual void DisposeVRageInstance()
        {
            GameInstance.Dispose();
            GameInstance = null;

            MyGameService.ShutDown();

            _getVRagePluginList().Remove(this);

            MyInitializer.InvokeAfterRun();
        }

        #endregion

        /// <inheritdoc/>
        public virtual Task Save(long callerId)
        {
            return SaveGameAsync(null);
        }

        /// <inheritdoc/> 
        public virtual void Start()
        {
            if (MySandboxGame.FatalErrorDuringInit)
            {
                Log.Warn($"Failed to start sandbox game: fatal error during init");
                return;
            }
            GameInstance.Run();
        }

        /// <inheritdoc />
        public virtual void Stop()
        {
            if (IsOnGameThread())
                MySandboxGame.Static.Exit();
            else
                InvokeBlocking(MySandboxGame.Static.Exit);
        }

        /// <inheritdoc />
        public abstract void Restart();

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