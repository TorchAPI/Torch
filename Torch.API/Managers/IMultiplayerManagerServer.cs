using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Torch.API.Managers
{
    /// <summary>
    /// API for multiplayer functions that exist on servers and lobbies
    /// </summary>
    public interface IMultiplayerManagerServer : IMultiplayerManagerBase
    {
        /// <summary>
        /// Kicks the player from the game.
        /// </summary>
        void KickPlayer(ulong steamId);

        /// <summary>
        /// Bans or unbans a player from the game.
        /// </summary>
        void BanPlayer(ulong steamId, bool banned = true);

        /// <summary>
        /// Promotes user if possible.
        /// </summary>
        /// <param name="steamId"></param>
        void PromoteUser(ulong steamId);

        /// <summary>
        /// Demotes user if possible.
        /// </summary>
        /// <param name="steamId"></param>
        void DemoteUser(ulong steamId);

        /// <summary>
        /// Gets a user's promote level.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        MyPromoteLevel GetUserPromoteLevel(ulong steamId);

        /// <summary>
        /// List of the banned SteamID's
        /// </summary>        
        IReadOnlyList<ulong> BannedPlayers { get; }

        /// <summary>
        /// Checks if the player with the given SteamID is banned.
        /// </summary>
        /// <param name="steamId">The SteamID of the player.</param>
        /// <returns>True if the player is banned; otherwise false.</returns>
        bool IsBanned(ulong steamId);

        /// <summary>
        /// Raised when a player is kicked. Passes with SteamID of kicked player.
        /// </summary>
        event Action<ulong> PlayerKicked;

        /// <summary>
        /// Raised when a player is banned or unbanned. Passes SteamID of player, and true if banned, false if unbanned.
        /// </summary>
        event Action<ulong, bool> PlayerBanned;

        /// <summary>
        /// Raised when a player is promoted or demoted. Passes SteamID of player, and new promote level.
        /// </summary>
        event Action<ulong, MyPromoteLevel> PlayerPromoted;
    }
}
