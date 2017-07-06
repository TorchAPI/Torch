using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Library.Collections;
using VRage.Network;

namespace Torch.API.Managers
{
    /// <summary>
    /// API for the network intercept.
    /// </summary>
    public interface INetworkManager : IManager
    {
        /// <summary>
        /// Register a network handler.
        /// </summary>
        void RegisterNetworkHandler(INetworkHandler handler);
    }

    /// <summary>
    /// Handler for multiplayer network messages.
    /// </summary>
    public interface INetworkHandler
    {
        /// <summary>
        /// Returns if the handler can process the call site.
        /// </summary>
        bool CanHandle(CallSite callSite);

        /// <summary>
        /// Processes a network message.
        /// </summary>
        bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj, MyPacket packet);
    }
}
