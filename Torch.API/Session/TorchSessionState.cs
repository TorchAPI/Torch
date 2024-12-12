namespace Torch.API.Session
{
    /// <summary>
    /// Represents the state of a <see cref="ITorchSession"/>
    /// </summary>
    public enum TorchSessionState
    {
        /// <summary>
        /// The session has been created, and is now loading.
        /// </summary>
        Loading,
        /// <summary>
        /// The session has loaded, and is now running.
        /// </summary>
        Loaded,
        /// <summary>
        /// The session was running, and is now unloading.
        /// </summary>
        Unloading,
        /// <summary>
        /// The session was unloading, and is now unloaded and stopped.
        /// </summary>
        Unloaded
    }

    /// <summary>
    /// Callback raised when a session's state changes
    /// </summary>
    /// <param name="session">The session who had a state change</param>
    /// <param name="newState">The session's new state</param>
    public delegate void TorchSessionStateChangedDel(ITorchSession session, TorchSessionState newState);
}
