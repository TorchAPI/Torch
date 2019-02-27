using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;

namespace Torch
{
    public static class MyPlayerCollectionExtensions
    {
        public static MyPlayer TryGetPlayerBySteamId(this MyPlayerCollection collection, ulong steamId, int serialId = 0)
        {
            long identity = collection.TryGetIdentityId(steamId, serialId);
            if (identity == 0)
                return null;
            if (!collection.TryGetPlayerId(identity, out MyPlayer.PlayerId playerId))
                return null;
            return collection.TryGetPlayerById(playerId, out MyPlayer player) ? player : null;
        }

        public static string TryGetPlayerName(this MyPlayerCollection collection, ulong steamId, int serialId = 0)
        {
            return collection.TryGetPlayerBySteamId(steamId, serialId)?.DisplayName ?? $"ID: {steamId}";
        }

        public static string TryGetPlayerName(this MyPlayerCollection collection, long identityId)
        {
            if (!collection.TryGetPlayerId(identityId, out MyPlayer.PlayerId playerId))
                return null;

            collection.TryGetPlayerById(playerId, out MyPlayer player);

            return player?.DisplayName ?? $"ID: {collection.TryGetSteamId(identityId)}";
        }
    }
}
