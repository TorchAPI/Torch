using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface IPlayer
    {
        ulong SteamId { get; }
        List<ulong> IdentityIds { get; }
        string Name { get; }
        ConnectionState State { get; }
        DateTime LastConnected { get; }
        void SetConnectionState(ConnectionState state);
    }
}
