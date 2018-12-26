using System;
using System.Collections.Generic;
using System.Text;

namespace Torch.Mod.Messages
{
    /// <summary>
    /// shim to store incoming message data
    /// </summary>
    internal class IncomingMessage : MessageBase
    {
        public IncomingMessage()
        {
        }

        public override void ProcessClient()
        {
            throw new Exception();
        }

        public override void ProcessServer()
        {
            throw new Exception();
        }
    }
}
