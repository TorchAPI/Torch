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
        void SendMessage(string message, string author = "Server", long playerId = 0, string font = MyFontEnum.Blue);
        void KickPlayer(ulong id);
        void BanPlayer(ulong id, bool banned = true);
        IMyPlayer GetPlayerBySteamId(ulong id);
        IMyPlayer GetPlayerByName(string name);
    }
}