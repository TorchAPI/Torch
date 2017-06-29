using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Torch.API
{
    /// <summary>
    /// Represents a player on the server.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// The name of the player.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The SteamID of the player.
        /// </summary>
        ulong SteamId { get; }

        /// <summary>
        /// The player's current connection state.
        /// </summary>
        ConnectionState State { get; }
    }
}
