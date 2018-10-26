using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorkshopService
{
    public static class Util
    {
        public static DateTime DateTimeFromUnixTimeStamp(ulong seconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(seconds);
        }
    }
}
