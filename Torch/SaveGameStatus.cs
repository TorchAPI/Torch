namespace Torch
{
    /// <summary>
    /// Describes the possible outcomes when attempting to save the game progress.
    /// </summary>
    public enum SaveGameStatus : byte
    {
        Success = 0,
        SaveInProgress = 1,
        GameNotReady = 2,
        TimedOut = 3
    };
}
