using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.Library.Collections;
using VRage.Network;

namespace Torch.Managers.ChatManager
{
    [PatchShim]
    internal static class ChatInterceptPatch
    {
        private static ChatManagerServer _chatManager;
        private static ChatManagerServer ChatManager => _chatManager ?? (_chatManager = TorchBase.Instance.Managers.GetManager<ChatManagerServer>());
            
        internal static void Patch(PatchContext context)
        {
            var target = typeof(MyMultiplayerBase).GetMethod("OnChatMessageRecieved_Server", BindingFlags.Static | BindingFlags.NonPublic);
            var patchMethod = typeof(ChatInterceptPatch).GetMethod(nameof(PrefixMessageProcessing), BindingFlags.Static | BindingFlags.NonPublic);
            context.GetPattern(target).Prefixes.Add(patchMethod);
        }

        private static void PrefixMessageProcessing(ref ChatMsg msg)
        {
            var consumed = false;
            ChatManager?.RaiseMessageRecieved(msg, ref consumed);
        }
    }
    
    public class ChatManagerServer : ChatManagerClient, IChatManagerServer
    {
        [Dependency(Optional = true)]
        private INetworkManager _networkManager;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly Logger _chatLog = LogManager.GetLogger("Chat");

        /// <inheritdoc />
        public ChatManagerServer(ITorchBase torchInstance) : base(torchInstance)
        {
            
        }

        /// <inheritdoc />
        public event MessageProcessingDel MessageProcessing;

        /// <inheritdoc />
        public void SendMessageAsOther(ulong authorId, string message, ulong targetSteamId = 0)
        {
            if (targetSteamId == Sync.MyId)
            {
                RaiseMessageRecieved(new TorchChatMessage(authorId, message));
                return;
            }
            if (MyMultiplayer.Static == null)
            {
                if ((targetSteamId == MyGameService.UserId || targetSteamId == 0) && HasHud)
                    MyHud.Chat?.ShowMessage(authorId == MyGameService.UserId ?
                        (MySession.Static.LocalHumanPlayer?.DisplayName ?? "Player") : $"user_{authorId}", message);
                return;
            }
            if (MyMultiplayer.Static is MyDedicatedServerBase dedicated)
            {
                var msg = new ChatMsg() { Author = authorId, Text = message };
                _dedicatedServerBaseSendChatMessage.Invoke(ref msg);
                _dedicatedServerBaseOnChatMessage.Invoke(dedicated, new object[] { msg });
            }
        }


#pragma warning disable 649
        private delegate void MultiplayerBaseSendChatMessageDel(ref ChatMsg arg);
        [ReflectedStaticMethod(Name = "SendChatMessage", Type = typeof(MyMultiplayerBase))]
        private static MultiplayerBaseSendChatMessageDel _dedicatedServerBaseSendChatMessage;

        // [ReflectedMethod] doesn't play well with instance methods and refs.
        [ReflectedMethodInfo(typeof(MyDedicatedServerBase), "OnChatMessage")]
        private static MethodInfo _dedicatedServerBaseOnChatMessage;
#pragma warning restore 649

        /// <inheritdoc />
        public void SendMessageAsOther(string author, string message, string font, ulong targetSteamId = 0)
        {
            if (targetSteamId == Sync.MyId)
            {
                RaiseMessageRecieved(new TorchChatMessage(author, message, font));
                return;
            }
            if (MyMultiplayer.Static == null)
            {
                if ((targetSteamId == MyGameService.UserId || targetSteamId == 0) && HasHud)
                    MyHud.Chat?.ShowMessage(author, message, font);
                return;
            }
            var scripted = new ScriptedChatMsg()
            {
                Author = author,
                Text = message,
                Font = font,
                Target = Sync.Players.TryGetIdentityId(targetSteamId)
            };
            _chatLog.Info($"{author} (to {GetMemberName(targetSteamId)}): {message}");
            MyMultiplayerBase.SendScriptedChatMessage(ref scripted);
        }


        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();

            if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.ChatMessageReceived += MpStaticChatMessageReceived;
                _log.Warn(
                    "Failed to initialize network intercept, we can't discard chat messages! Falling back to another method.");
            }
            else
            {
                _log.Debug("Using offline message processor");
            }
        }

        /// <inheritdoc />
        protected override bool OfflineMessageProcessor(TorchChatMessage msg)
        {
            if (MyMultiplayer.Static != null)
                return false;
            var consumed = false;
            MessageProcessing?.Invoke(msg, ref consumed);
            return consumed;
        }

        private void MpStaticChatMessageReceived(ulong steamUserId, string messageText, ChatChannel channel, long targetId, string customAuthorName)
        {
            var tmp = false;
            RaiseMessageRecieved(new ChatMsg
            {
                Author = steamUserId,
                Text = messageText
            }, ref tmp);
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            if (MyMultiplayer.Static != null)
                MyMultiplayer.Static.ChatMessageReceived -= MpStaticChatMessageReceived;
            base.Detach();
        }

        internal void RaiseMessageRecieved(ChatMsg message, ref bool consumed)
        {
            var torchMsg = new TorchChatMessage(GetMemberName(message.Author), message.Author, message.Text);
            MessageProcessing?.Invoke(torchMsg, ref consumed);

            if (!consumed)
                _chatLog.Info($"{torchMsg.Author}: {torchMsg.Message}");
        }

        public static string GetMemberName(ulong steamId)
        {
            return MySession.Static.Players.TryGetPlayerName(steamId);
        }
    }
}
