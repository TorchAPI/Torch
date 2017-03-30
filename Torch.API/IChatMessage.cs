using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface IChatMessage
    {
        DateTime Timestamp { get; }
        ulong SteamId { get; }
        string Name { get; }
        string Message { get; }
    }
}
