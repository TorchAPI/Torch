using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Collections;
using VRage.Network;

namespace Torch.API.Managers
{

    /// <summary>
    /// Callback used to indicate the server has recieved a message to process and forward on to others.
    /// </summary>
    /// <param name="authorId">Steam ID of the user sending a message</param>
    /// <param name="msg">Message the user is attempting to send</param>
    /// <param name="consumed">If true, this event has been consumed and should be ignored</param>
    public delegate void MessageProcessingDel(TorchChatMessage msg, ref bool consumed);

    public interface IChatManagerServer : IChatManagerClient
    {
        /// <summary>
        /// Event triggered when the server has recieved a message and should process it.  <see cref="MessageProcessingDel"/>
        /// </summary>
        event MessageProcessingDel MessageProcessing;


        /// <summary>
        /// Sends a message with the given author and message to the given player, or all players by default.
        /// </summary>
        /// <param name="authorId">Author's steam ID</param>
        /// <param name="message">The message to send</param>
        /// <param name="targetSteamId">Player to send the message to, or everyone by default</param>
        void SendMessageAsOther(ulong authorId, string message, ulong targetSteamId = 0);


        /// <summary>
        /// Sends a scripted message with the given author and message to the given player, or all players by default.
        /// </summary>
        /// <param name="author">Author name</param>
        /// <param name="message">The message to send</param>
        /// <param name="font">Font to use</param>
        /// <param name="targetSteamId">Player to send the message to, or everyone by default</param>
        void SendMessageAsOther(string author, string message, string font, ulong targetSteamId = 0);

        /// <summary>
        /// Mute user from global chat.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        bool MuteUser(ulong steamId);

        /// <summary>
        /// Unmute user from global chat.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        bool UnmuteUser(ulong steamId);

        /// <summary>
        /// Users which are not allowed to chat.
        /// </summary>
        HashSetReader<ulong> MutedUsers { get; }
    }
}
