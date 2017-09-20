using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// List of the banned SteamID's
        /// </summary>        
        IReadOnlyList<ulong> BannedPlayers { get; }

        /// <summary>
        /// Checks if the player with the given SteamID is banned.
        /// </summary>
        /// <param name="steamId">The SteamID of the player.</param>
        /// <returns>True if the player is banned; otherwise false.</returns>
        bool IsBanned(ulong steamId);
    }
}
