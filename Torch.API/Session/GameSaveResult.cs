using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Session
{
    /// <summary>
    /// The result of a save operation
    /// </summary>
    public enum GameSaveResult
    {
        /// <summary>
        /// Successfully saved
        /// </summary>
        Success = 0,

        /// <summary>
        /// The game wasn't ready to be saved
        /// </summary>
        GameNotReady = -1,

        /// <summary>
        /// Failed to take the snapshot of the current world state
        /// </summary>
        FailedToTakeSnapshot = -2,

        /// <summary>
        /// Failed to save the snapshot to disk
        /// </summary>
        FailedToSaveToDisk = -3,

        /// <summary>
        /// An unknown error occurred
        /// </summary>
        UnknownError = -4,

        /// <summary>
        /// The save operation timed out
        /// </summary>
        TimedOut = -5
    }
}