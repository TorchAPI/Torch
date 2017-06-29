using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface IChatMessage
    {
        /// <summary>
        /// The time the message was created.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The SteamID of the message author.
        /// </summary>
        ulong SteamId { get; }

        /// <summary>
        /// The name of the message author.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        string Message { get; }
    }
}
