using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.API.Managers
{
    /// <summary>
    /// Delegate for received messages.
    /// </summary>
    /// <param name="message">Message data.</param>
    /// <param name="sendToOthers">Flag to broadcast message to other players.</param>
    public delegate void MessageReceivedDel(IChatMessage message, ref bool sendToOthers);

    /// <summary>
    /// API for multiplayer related functions.
    /// </summary>
    public interface IMultiplayerManagerBase : IManager
    {
        /// <summary>
        /// Fired when a player joins.
        /// </summary>
        event Action<IPlayer> PlayerJoined;

        /// <summary>
        /// Fired when a player disconnects.
        /// </summary>
        event Action<IPlayer> PlayerLeft;
        
        /// <summary>
        /// Gets a player by their Steam64 ID or returns null if the player isn't found.
        /// </summary>
        IMyPlayer GetPlayerBySteamId(ulong id);

        /// <summary>
        /// Gets a player by their display name or returns null if the player isn't found.
        /// </summary>
        IMyPlayer GetPlayerByName(string name);

        /// <summary>
        /// Gets the steam username of a member's steam ID
        /// </summary>
        /// <param name="steamId">steam ID</param>
        /// <returns>steam username</returns>
        string GetSteamUsername(ulong steamId);
    }
}