using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRage.Game;
using VRage.Game.ModAPI;

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
        void SendMessage(string message, long playerId, string author = "Server", string font = MyFontEnum.Blue);
        void KickPlayer(ulong id);
        void BanPlayer(ulong id, bool banned = true);
        IMyPlayer GetPlayerBySteamId(ulong id);
    }
}