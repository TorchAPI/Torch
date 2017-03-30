using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.API
{
    public delegate void MessageReceivedDel(IChatMessage message, ref bool sendToOthers);

    public interface IMultiplayer
    {
        event Action<ulong> PlayerJoined;
        event Action<ulong, ConnectionState> PlayerLeft;
        event MessageReceivedDel MessageReceived;
        void SendMessage(string message, string author = "Server", long playerId = 0, string font = MyFontEnum.Blue);
        void KickPlayer(ulong steamId);
        void BanPlayer(ulong steamId, bool banned = true);
        IMyPlayer GetPlayerBySteamId(ulong id);
        IMyPlayer GetPlayerByName(string name);
        MTObservableCollection<IChatMessage> ChatHistory { get; }
    }
}