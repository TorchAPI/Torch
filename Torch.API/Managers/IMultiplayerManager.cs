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
    public interface IMultiplayerManager : IManager
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
        /// Fired when a chat message is received.
        /// </summary>
        event MessageReceivedDel MessageReceived;

        /// <summary>
        /// List of banned SteamID's
        /// </summary>
        List<ulong> BannedPlayers { get; }

        /// <summary>
        /// Send a chat message to all or one specific player.
        /// </summary>
        void SendMessage(string message, string author = "Server", long playerId = 0, string font = MyFontEnum.Blue);

        /// <summary>
        /// Kicks the player from the game.
        /// </summary>
        void KickPlayer(ulong steamId);

        /// <summary>
        /// Bans or unbans a player from the game.
        /// </summary>
        void BanPlayer(ulong steamId, bool banned = true);

        /// <summary>
        /// Gets a player by their Steam64 ID or returns null if the player isn't found.
        /// </summary>
        IMyPlayer GetPlayerBySteamId(ulong id);

        /// <summary>
        /// Gets a player by their display name or returns null if the player isn't found.
        /// </summary>
        IMyPlayer GetPlayerByName(string name);
    }
}