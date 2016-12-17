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
        public static MyPlayer TryGetPlayerBySteamId(this MyPlayerCollection collection, ulong steamId)
        {
            var id = collection.GetAllPlayers().FirstOrDefault(x => x.SteamId == steamId);
            return id == default(MyPlayer.PlayerId) ? null : collection.GetPlayerById(id);
        }
    }
}
