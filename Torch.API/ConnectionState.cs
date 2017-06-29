using System;

namespace Torch.API
{
    /// <summary>
    /// Identifies a player's current connection state.
    /// </summary>
    [Flags]
    public enum ConnectionState
    {
        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown,

        /// <summary>
        /// Connected to game.
        /// </summary>
        Connected = 1,

        /// <summary>
        /// Left the game.
        /// </summary>
        Left = 2,

        /// <summary>
        /// Disconnected from the game.
        /// </summary>
        Disconnected = 4,

        /// <summary>
        /// Kicked from the game.
        /// </summary>
        Kicked = 8,

        /// <summary>
        /// Banned from the game.
        /// </summary>
        Banned = 16,
    }
}