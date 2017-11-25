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
        event Action SessionLoading;
        
        /// <summary>
        /// Fired when the session finishes loading.
        /// </summary>
        event Action SessionLoaded;

        /// <summary>
        /// Fires when the session begins unloading.
        /// </summary>
        event Action SessionUnloading;

        /// <summary>
        /// Fired when the session finishes unloading.
        /// </summary>
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
        Version TorchVersion { get; }

        /// <summary>
        /// Invoke an action on the game thread.
        /// </summary>
        void Invoke(Action action, [CallerMemberName] string caller = "");

        /// <summary>
        /// Invoke an action on the game thread and block until it has completed.
        /// If this is called on the game thread the action will execute immediately.
        /// </summary>
        void InvokeBlocking(Action action, [CallerMemberName] string caller = "");

        /// <summary>
        /// Invoke an action on the game thread asynchronously.
        /// </summary>
        Task InvokeAsync(Action action, [CallerMemberName] string caller = "");

        /// <summary>
        /// Start the Torch instance.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the Torch instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Restart the Torch instance.
        /// </summary>
        void Restart();

        /// <summary>
        /// Initializes a save of the game.
        /// </summary>
        /// <param name="callerId">Id of the player who initiated the save.</param>
        Task Save(long callerId);

        /// <summary>
        /// Initialize the Torch instance.  Before this <see cref="Start"/> is invalid.
        /// </summary>
        void Init();

        /// <summary>
        /// Disposes the Torch instance.  After this <see cref="Start"/> is invalid.
        /// </summary>
        void Dispose();

        /// <summary>
        /// The current state of the game this instance of torch is controlling.
        /// </summary>
        TorchGameState GameState { get; }

        /// <summary>
        /// Event raised when <see cref="GameState"/> changes.
        /// </summary>
        event TorchGameStateChangedDel GameStateChanged;
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
    }

    /// <summary>
    /// API for the Torch client.
    /// </summary>
    public interface ITorchClient : ITorchBase
    {

    }
}
