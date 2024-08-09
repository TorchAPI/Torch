using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamKit2.Unified.Internal;
using Torch.API;
using Torch.API.Managers;
using Torch.Utils;
using VRage.Game;
using VRage.GameServices;
using VRageMath;
using Color = VRageMath.Color;

namespace Torch.Managers.ChatManager
{
    public class ChatManagerClient : Manager, IChatManagerClient
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public ChatManagerClient(ITorchBase torchInstance) : base(torchInstance) { }

        /// <inheritdoc />
        public event MessageRecievedDel MessageRecieved;

        /// <inheritdoc />
        public event MessageSendingDel MessageSending;
        
        /// <inheritdoc />
        public void SendMessageAsSelf(string message)
        {
            if (MyMultiplayer.Static != null)
            {
                if (Sandbox.Engine.Platform.Game.IsDedicated)
                {
                    var scripted = new ScriptedChatMsg()
                    {
                        Author = Torch.Config.ChatName,
                        Text = message,
                        Target = 0
                    };
                    
                    var color = Torch.Config.ChatColor;
                    if (StringUtils.IsFontEnum(color))
                        scripted.Font = color;
                    else
                        scripted.Font = MyFontEnum.White;

                    scripted.Color = ColorUtils.TranslateColor(color);
                    
                    MyMultiplayerBase.SendScriptedChatMessage(ref scripted);
                }
                else
                    throw new NotImplementedException("Chat system changes broke this");
                    //MyMultiplayer.Static.SendChatMessage(message);
            }
            else if (HasHud)
                MyHud.Chat.ShowMessage(MySession.Static.LocalHumanPlayer?.DisplayName ?? "Player", message);
        }

        /// <inheritdoc />
        public void DisplayMessageOnSelf(string author, string message, string font)
        {
            if (HasHud)
                MyHud.Chat?.ShowMessage(author, message, font);
        }

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            MyAPIUtilities.Static.MessageEntered += OnMessageEntered;
            if (MyMultiplayer.Static != null)
            {
                _chatMessageRecievedReplacer = _chatMessageReceivedFactory.Invoke();
                _scriptedChatMessageRecievedReplacer = _scriptedChatMessageReceivedFactory.Invoke();
                _chatMessageRecievedReplacer.Replace(new Action<ulong, string, ChatChannel, long, ChatMessageCustomData?>(Multiplayer_ChatMessageReceived), MyMultiplayer.Static);
                _scriptedChatMessageRecievedReplacer.Replace(new Action<string, string, string, Color>(Multiplayer_ScriptedChatMessageReceived), MyMultiplayer.Static);
            }
            else
            {
                MyAPIUtilities.Static.MessageEntered += OfflineMessageReciever;
            }
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            MyAPIUtilities.Static.MessageEntered -= OnMessageEntered;
            if (_chatMessageRecievedReplacer != null && _chatMessageRecievedReplacer.Replaced && HasHud)
                _chatMessageRecievedReplacer.Restore(MyHud.Chat);
            if (_scriptedChatMessageRecievedReplacer != null && _scriptedChatMessageRecievedReplacer.Replaced && HasHud)
                _scriptedChatMessageRecievedReplacer.Restore(MyHud.Chat);
            MyAPIUtilities.Static.MessageEntered -= OfflineMessageReciever;
            base.Detach();
        }

        /// <summary>
        /// Callback used to process offline messages.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>true if the message was consumed</returns>
        protected virtual bool OfflineMessageProcessor(TorchChatMessage msg)
        {
            return false;
        }

        private void OfflineMessageReciever(string messageText, ref bool sendToOthers)
        {
            if (!sendToOthers)
                return;
            var torchMsg = new TorchChatMessage(MySession.Static.LocalHumanPlayer?.DisplayName ?? "Player", Sync.MyId, messageText, ChatChannel.Global, 0);
            bool consumed = RaiseMessageRecieved(torchMsg);
            if (!consumed)
                consumed = OfflineMessageProcessor(torchMsg);
            sendToOthers = !consumed;
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!sendToOthers)
                return;
            var consumed = false;
            MessageSending?.Invoke(messageText, ref consumed);
            sendToOthers = !consumed;
        }


        private void Multiplayer_ChatMessageReceived(ulong steamUserId, string messageText, ChatChannel channel, long targetId, ChatMessageCustomData? customData )
        {
            var torchMsg = new TorchChatMessage(steamUserId, messageText, channel, targetId,
                (steamUserId == MyGameService.UserId) ? MyFontEnum.DarkBlue : MyFontEnum.Blue);
            if (!RaiseMessageRecieved(torchMsg) && HasHud)
                _hudChatMessageReceived.Invoke(MyHud.Chat, steamUserId, messageText, channel, targetId, customData);
        }

        private void Multiplayer_ScriptedChatMessageReceived(string message, string author, string font, Color color)
        {
            var torchMsg = new TorchChatMessage(author, message, font);
            if (!RaiseMessageRecieved(torchMsg) && HasHud)
                _hudChatScriptedMessageReceived.Invoke(MyHud.Chat, author, message, font, color);
        }

        protected bool RaiseMessageRecieved(TorchChatMessage msg)
        {
            var consumed = false;
            MessageRecieved?.Invoke(msg, ref consumed);
            return consumed;
        }

        private const string _hudChatMessageReceivedName = "OnMultiplayer_ChatMessageReceived";
        private const string _hudChatScriptedMessageReceivedName = "multiplayer_ScriptedChatMessageReceived";
        
        protected static bool HasHud => !Sandbox.Engine.Platform.Game.IsDedicated;

        [ReflectedMethod(Name = _hudChatMessageReceivedName)]
        private static Action<MyHudChat, ulong, string, ChatChannel, long, ChatMessageCustomData?> _hudChatMessageReceived;
        [ReflectedMethod(Name = _hudChatScriptedMessageReceivedName)]
        private static Action<MyHudChat, string, string, string, Color> _hudChatScriptedMessageReceived;

        [ReflectedEventReplace(typeof(MyMultiplayerBase), nameof(MyMultiplayerBase.ChatMessageReceived), typeof(MyHudChat), _hudChatMessageReceivedName)]
        private static Func<ReflectedEventReplacer> _chatMessageReceivedFactory;
        [ReflectedEventReplace(typeof(MyMultiplayerBase), nameof(MyMultiplayerBase.ScriptedChatMessageReceived), typeof(MyHudChat), _hudChatScriptedMessageReceivedName)]
        private static Func<ReflectedEventReplacer> _scriptedChatMessageReceivedFactory;

        private ReflectedEventReplacer _chatMessageRecievedReplacer;
        private ReflectedEventReplacer _scriptedChatMessageRecievedReplacer;
    }
}
