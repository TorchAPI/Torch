using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piston
{
    public static class PlayerInfoCache
    {
        private static readonly Dictionary<ulong, PlayerInfo> _cache = new Dictionary<ulong, PlayerInfo>();

        public static PlayerInfo GetOrCreate(ulong steamId)
        {
            PlayerInfo info;
            if (_cache.TryGetValue(steamId, out info))
                return info;

            info = new PlayerInfo(steamId) {State = ConnectionState.Unknown};
            _cache.Add(steamId, info);
            return info;
        }

        public static void Add(PlayerInfo info)
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
