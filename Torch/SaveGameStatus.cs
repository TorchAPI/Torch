namespace Torch
{
    /// <summary>
    /// Describes the possible outcomes when attempting to save the game progress.
    /// </summary>
    public enum SaveGameStatus : byte
    {
        /// <summary>
        /// The game was saved.
        /// </summary>
        Success = 0,

        /// <summary>
        /// A save operation is already in progress.
        /// </summary>
        SaveInProgress = 1,

        /// <summary>
        /// The game is not in a save-able state.
        /// </summary>
        GameNotReady = 2,

        /// <summary>
        /// The save operation timed out.
        /// </summary>
        TimedOut = 3
    };
}
