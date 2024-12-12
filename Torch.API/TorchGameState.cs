using Sandbox;

namespace Torch.API
{
    /// <summary>
    /// Represents the state of a <see cref="MySandboxGame"/>
    /// </summary>
    public enum TorchGameState
    {
        /// <summary>
        /// The game is currently being created.
        /// </summary>
        Creating,
        /// <summary>
        /// The game has been created and is ready to begin loading.
        /// </summary>
        Created,
        /// <summary>
        /// The game is currently loading.
        /// </summary>
        Loading,
        /// <summary>
        /// The game is fully loaded and ready to start sessions
        /// </summary>
        Loaded,
        /// <summary>
        /// The game is beginning the unload sequence
        /// </summary>
        Unloading,
        /// <summary>
        /// The game has been shutdown and is no longer active
        /// </summary>
        Unloaded
    }
    
    /// <summary>
    /// Callback raised when a game's state changes
    /// </summary>
    /// <param name="game">The game who had a state change</param>
    /// <param name="newState">The game's new state</param>
    public delegate void TorchGameStateChangedDel(MySandboxGame game, TorchGameState newState);
}
