using System;

namespace Piston.Server
{
    /// <summary>
    /// Identifies a player's current connection state.
    /// </summary>
    [Flags]
    public enum ConnectionState
    {
        Unknown,
        Connected = 1,
        Left = 2,
        Disconnected = 4,
        Kicked = 8,
        Banned = 16,
    }
}