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
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game;
using Torch.API;
using Torch.Commands;
using Torch.Managers;
using VRage.FileSystem;
using VRage.Game.ObjectBuilder;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;

namespace Torch
{
    public abstract class TorchBase : ViewModel, ITorchBase, IPlugin
    {
        /// <summary>
        /// Hack because *keen*.
        /// Use only if necessary, prefer dependency injection.
        /// </summary>
        public static ITorchBase Instance { get; private set; }
        public ITorchConfig Config { get; protected set; }
        protected static Logger Log { get; } = LogManager.GetLogger("Torch");
        public Version TorchVersion { get; protected set; }
        public Version GameVersion { get; private set; }
        public string[] RunArgs { get; set; }
        public IPluginManager Plugins { get; protected set; }
        public IMultiplayer Multiplayer { get; protected set; }
        public EntityManager Entities { get; protected set; }
        public NetworkManager Network { get; protected set; }
        public CommandManager Commands { get; protected set; }
        public event Action SessionLoading;
        public event Action SessionLoaded;
        public event Action SessionUnloading;
        public event Action SessionUnloaded;

        private bool _init;

        protected TorchBase()
        {
            if (Instance != null)
                throw new InvalidOperationException("A TorchBase instance already exists.");

            Instance = this;

            TorchVersion = Assembly.GetExecutingAssembly().GetName().Version; 
            RunArgs = new string[0];
            Plugins = new PluginManager(this);
            Multiplayer = new MultiplayerManager(this);
            Entities = new EntityManager(this);
            Network = NetworkManager.Instance;
            Commands = new CommandManager(this);
        }

        public bool IsOnGameThread()
        {
            return Thread.CurrentThread.ManagedThreadId == MySandboxGame.Static.UpdateThread.ManagedThreadId;
        }

        public async Task SaveGameAsync()
        {
            Log.Info("Saving game");
            if (MySandboxGame.IsGameReady && !MyAsyncSaving.InProgress && Sync.IsServer && !(MySession.Static.LocalCharacter?.IsDead ?? true))
            {
                using (var e = new AutoResetEvent(false))
                {
                    MyAsyncSaving.Start(() =>
                    {
                        MySector.ResetEyeAdaptation = true;
                        e.Set();
                    });

                    await Task.Run(() =>
                    {
                        if (!e.WaitOne(60000))
                        {
                            Log.Error("Save failed!");
                            Multiplayer.SendMessage("Save timed out!", author: "Error");
                        }
                    });
                }
            }
            else
            {
                Log.Error("Cannot save");
            }
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

        public virtual void Init()
        {
            Debug.Assert(!_init, "Torch instance is already initialized.");

            SpaceEngineersGame.SetupBasicGameInfo();
            SpaceEngineersGame.SetupPerGameSettings();

            /*
            if (Directory.Exists("DedicatedServer64"))
            {
                Log.Debug("Inserting DedicatedServer64 before MyPerGameSettings assembly paths");
                MyPerGameSettings.GameModAssembly = $"DedicatedServer64\\{MyPerGameSettings.GameModAssembly}";
                MyPerGameSettings.GameModObjBuildersAssembly = $"DedicatedServer64\\{MyPerGameSettings.GameModObjBuildersAssembly}";
                MyPerGameSettings.SandboxAssembly = $"DedicatedServer64\\{MyPerGameSettings.SandboxAssembly}";
                MyPerGameSettings.SandboxGameAssembly = $"DedicatedServer64\\{MyPerGameSettings.SandboxGameAssembly}";
            }*/

            TorchVersion = Assembly.GetEntryAssembly().GetName().Version;
            GameVersion = new Version(new MyVersion(MyPerGameSettings.BasicGameInfo.GameVersion.Value).FormattedText.ToString().Replace("_", "."));
            var verInfo = $"Torch {TorchVersion}, SE {GameVersion}";
            Console.Title = verInfo;
#if DEBUG
            Log.Info("DEBUG");
#else
            Log.Info("RELEASE");
#endif
            Log.Info(verInfo);
            Log.Info($"Executing assembly: {Assembly.GetEntryAssembly().FullName}");
            Log.Info($"Executing directory: {AppDomain.CurrentDomain.BaseDirectory}");

            MySession.OnLoading += () => SessionLoading?.Invoke();
            MySession.AfterLoading += () => SessionLoaded?.Invoke();
            MySession.OnUnloading += () => SessionUnloading?.Invoke();
            MySession.OnUnloaded += () => SessionUnloaded?.Invoke();
            RegisterVRagePlugin();

            _init = true;
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

        public virtual void Start()
        {
            Plugins.Init();
        }

        public virtual void Stop() { }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Plugins.DisposePlugins();
        }

        /// <inheritdoc />
        public virtual void Init(object gameInstance)
        {
            Network.Init();
            ChatManager.Instance.Init();
        }

        /// <inheritdoc />
        public virtual void Update()
        {
            Plugins.UpdatePlugins();
        }
    }
}
