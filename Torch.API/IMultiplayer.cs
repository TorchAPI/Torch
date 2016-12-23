using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Torch.API
{
    public interface IMultiplayer
    {
        event Action<IPlayer> PlayerJoined;
        event Action<IPlayer> PlayerLeft;
        event Action<IChatItem> MessageReceived;
        Dictionary<ulong, IPlayer> Players { get; }
        List<IChatItem> Chat { get; }
        void SendMessage(string message);
        void KickPlayer(ulong id);
        void BanPlayer(ulong id, bool banned = true);
    }
}