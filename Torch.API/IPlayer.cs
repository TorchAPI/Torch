using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Torch.API
{
    public interface IPlayer
    {
        string Name { get; }
        ulong SteamId { get; }
        ConnectionState State { get; }
    }
}
