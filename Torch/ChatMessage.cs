using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Torch.API;
using VRage.Network;

namespace Torch
{
    public class ChatMessage : IChatMessage
    {
        public DateTime Timestamp { get; }
        public ulong SteamId { get; }
        public string Name { get; }
        public string Message { get; }

        public ChatMessage(DateTime timestamp, ulong steamId, string name, string message)
        {
            Timestamp = timestamp;
            SteamId = steamId;
            Name = name;
            Message = message;
        }

        public static ChatMessage FromChatMsg(ChatMsg msg, DateTime dt = default(DateTime))
        {
            return new ChatMessage(
                dt == default(DateTime) ? DateTime.Now : dt, 
                msg.Author,
                MyMultiplayer.Static.GetMemberName(msg.Author),
                msg.Text);
        }
    }
}
