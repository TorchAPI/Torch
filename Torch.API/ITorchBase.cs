using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <inheritdoc cref="IMultiplayerManager"/>
        [Obsolete]
        IMultiplayerManager Multiplayer { get; }

        /// <inheritdoc cref="IPluginManager"/>
        [Obsolete]
        IPluginManager Plugins { get; }

        /// <inheritdoc cref="IDependencyManager"/>
        IDependencyManager Managers { get; }

        /// <summary>
        /// The binary version of the current instance.
        /// </summary>
        Version TorchVersion { get; }

        /// <summary>
        /// Invoke an action on the game thread.
        /// </summary>
        void Invoke(Action action);

        /// <summary>
        /// Invoke an action on the game thread and block until it has completed.
        /// If this is called on the game thread the action will execute immediately.
        /// </summary>
        void InvokeBlocking(Action action);

        /// <summary>
        /// Invoke an action on the game thread asynchronously.
        /// </summary>
        Task InvokeAsync(Action action);

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
        /// Initialize the Torch instance.
        /// </summary>
        void Init();
    }

    /// <summary>
    /// API for the Torch server.
    /// </summary>
    public interface ITorchServer : ITorchBase
    {
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
