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
    public interface INetworkManager : IManager
    {
        void RegisterNetworkHandler(INetworkHandler handler);
    }

    public interface INetworkHandler
    {
        bool CanHandle(CallSite callSite);
        bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj, MyPacket packet);
    }
}
