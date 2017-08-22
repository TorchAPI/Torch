using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;

namespace Torch.Client.Manager
{
    public class MultiplayerManagerClient : MultiplayerManagerBase, IMultiplayerManagerClient
    {
        /// <inheritdoc />
        public MultiplayerManagerClient(ITorchBase torch) : base(torch) { }

        /// <inheritdoc />
        public override void Attach()
        {
            base.Attach();
            MyMultiplayer.Static.ClientJoined += RaiseClientJoined;
        }

        /// <inheritdoc />
        public override void Detach()
        {
            MyMultiplayer.Static.ClientJoined -= RaiseClientJoined;
            base.Detach();
        }
    }
}
