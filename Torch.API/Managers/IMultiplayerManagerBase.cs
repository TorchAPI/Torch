using System;
using VRage.Game.ModAPI;

namespace Torch.API.Managers
{
    /// <summary>
    /// API for multiplayer related functions common to servers and clients.
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