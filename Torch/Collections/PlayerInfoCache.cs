using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;

namespace Torch
{
    public static class PlayerInfoCache
    {
        private static readonly Dictionary<ulong, Player> _cache = new Dictionary<ulong, Player>();

        public static Player GetOrCreate(ulong steamId)
        {
            if (_cache.TryGetValue(steamId, out Player info))
                return info;

            info = new Player(steamId) {State = ConnectionState.Unknown};
            _cache.Add(steamId, info);
            return info;
        }

        public static void Add(Player info)
        {
            if (_cache.ContainsKey(info.SteamId))
                return;

            _cache.Add(info.SteamId, info);
        }

        public static void Reset()
        {
            _cache.Clear();
        }
    }
}
