using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Torch.API.Managers;
using Torch.API.Session;
using VRage.Game.ModAPI;

namespace Torch.API
{
    /// <summary>
    /// API for Torch functions shared between client and server.
    /// </summary>
    public interface ITorchBase
    {
        /// <summary>
        /// Fired when the session begins loading.
        /// </summary>
        [Obsolete("Prefer using the TorchSessionManager.SessionStateChanged event")]
        event Action SessionLoading;

        /// <summary>
        /// Fired when the session finishes loading.
        /// </summary>
        [Obsolete("Prefer using the TorchSessionManager.SessionStateChanged event")]
        event Action SessionLoaded;

        /// <summary>
        /// Fires when the session begins unloading.
        /// </summary>
        [Obsolete("Prefer using the TorchSessionManager.SessionStateChanged event")]
        event Action SessionUnloading;

        /// <summary>
        /// Fired when the session finishes unloading.
        /// </summary>
        [Obsolete("Prefer using the TorchSessionManager.SessionStateChanged event")]
        event Action SessionUnloaded;

        /// <summary>
        /// Gets the currently running session instance, or null if none exists.
        /// </summary>
        ITorchSession CurrentSession { get; }

        /// <summary>
        /// Configuration for the current instance.
        /// </summary>
        ITorchConfig Config { get; }

        /// <inheritdoc cref="IPluginManager"/>
        [Obsolete]
        IPluginManager Plugins { get; }

        /// <inheritdoc cref="IDependencyManager"/>
        IDependencyManager Managers { get; }

        [Obsolete("Prefer using Managers.GetManager for global managers")]
        T GetManager<T>() where T : class, IManager;

        [Obsolete("Prefer using Managers.AddManager for global managers")]
        bool AddManager<T>(T manager) where T : class, IManager;

        /// <summary>
        /// The binary version of the current instance.
        /// </summary>
        InformationalVersion TorchVersion { get; }

        /// <summary>
        /// Invoke an action on the game thread.
        /// </summary>
        void Invoke(Action action, [CallerMemberName] string caller = "");

        /// <summary>
        /// Invoke an action on the game thread and block until it has completed.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="caller">Caller of the invoke function</param>
        /// <param name="timeoutMs">Timeout before <see cref="TimeoutException"/> is thrown, or -1 to never timeout</param>
        /// <exception cref="TimeoutException">If the action times out</exception>
        void InvokeBlocking(Action action, int timeoutMs = -1, [CallerMemberName] string caller = "");

        /// <summary>
        /// Invoke an action on the game thread asynchronously.
        /// </summary>
        Task InvokeAsync(Action action, [CallerMemberName] string caller = "");

        /// <summary>
        /// Invoke a function on the game thread asynchronously.
        /// </summary>
        Task<T> InvokeAsync<T>(Func<T> func, [CallerMemberName] string caller = "");

        /// <summary>
        /// Signals the torch instance to start, then blocks until it's started.
        /// </summary>
        void Start();

        /// <summary>
        /// Signals the torch instance to stop, then blocks until it's stopped.
        /// </summary>
        void Stop();

        /// <summary>
        /// Restart the Torch instance, blocking until the restart has been performed.
        /// </summary>
        void Restart(bool save = true);

        /// <summary>
        /// Initializes a save of the game.
        /// </summary>
        /// <param name="timeoutMs">timeout before the save is treated as failed, or -1 for no timeout</param>
        /// <param name="exclusive">Only start saving if we aren't already saving</param>
        /// <returns>Future result of the save, or null if one is in progress and in exclusive mode</returns>
        Task<GameSaveResult> Save(int timeoutMs = -1, bool exclusive = false);

        /// <summary>
        /// Initialize the Torch instance.  Before this <see cref="Start"/> is invalid.
        /// </summary>
        void Init();

        /// <summary>
        /// Disposes the Torch instance.  After this <see cref="Start"/> is invalid.
        /// </summary>
        void Destroy();

        /// <summary>
        /// The current state of the game this instance of torch is controlling.
        /// </summary>
        TorchGameState GameState { get; }

        /// <summary>
        /// Event raised when <see cref="GameState"/> changes.
        /// </summary>
        event TorchGameStateChangedDel GameStateChanged;
        
        /// <summary>
        /// Has a restart been requested?
        /// </summary>
        bool IsRestartPending { get; set; }
    }

    /// <summary>
    /// API for the Torch server.
    /// </summary>
    public interface ITorchServer : ITorchBase
    {
        /// <summary>
        /// The current <see cref="ServerState"/>
        /// </summary>
        ServerState State { get; }

        /// <summary>
        /// Path of the dedicated instance folder.
        /// </summary>
        string InstancePath { get; }

        /// <summary>
        /// Raised when the server's Init() method has completed.
        /// </summary>
        event Action<ITorchServer> Initialized;
        
        TimeSpan ElapsedPlayTime { get; set; }
    }

    /// <summary>
    /// API for the Torch client.
    /// </summary>
    public interface ITorchClient : ITorchBase
    {

    }
}
