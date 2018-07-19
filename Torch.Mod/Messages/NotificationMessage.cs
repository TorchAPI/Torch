using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Torch.Mod.Messages
{
    [ProtoContract]
    public class NotificationMessage : MessageBase
    {
        [ProtoMember(201)]
        public string Message;
        [ProtoMember(202)]
        public string Font;
        [ProtoMember(203)]
        public int DisappearTimeMs;

        public NotificationMessage()
        { }

        public NotificationMessage(string message, int disappearTimeMs, string font)
        {
            Message = message;
            DisappearTimeMs = disappearTimeMs;
            Font = font;
        }

        public override void ProcessClient()
        {
            MyAPIGateway.Utilities.ShowNotification(Message, DisappearTimeMs, Font);
        }

        public override void ProcessServer()
        {
            throw new Exception();
        }
    }
}
