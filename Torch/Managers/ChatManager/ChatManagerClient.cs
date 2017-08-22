using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Utils;
using VRage.Game;
using Game = Sandbox.Engine.Platform.Game;

namespace Torch.Managers.ChatManager
{
    public class ChatManagerClient : Manager, IChatManagerClient
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public ChatManagerClient(ITorchBase torchInstance) : base(torchInstance) { }

        /// <inheritdoc />
        // TODO doesn't work in Offline worlds.  Method injection or network injection.
        public event DelMessageRecieved MessageRecieved;

        /// <inheritdoc />
        // TODO doesn't work at all.  Method injection or network injection.
        public event DelMessageSending MessageSending;

        /// <inheritdoc />
        public void SendMessageAsSelf(string message)
        {
            if (MyMultiplayer.Static != null)
            {
                if (Game.IsDedicated)
                {
                    var scripted = new ScriptedChatMsg()
                    {
                        Author = "Server",
                        Font = MyFontEnum.Red,
                        Text = message,
                        Target = 0
                    };
                    MyMultiplayerBase.SendScriptedChatMessage(ref scripted);
                }
                else
                    MyMultiplayer.Static.SendChatMessage(message);
            }
            else if (MyHud.Chat != null)
                MyHud.Chat.ShowMessage(MySession.Static.LocalHumanPlayer?.DisplayName ?? "Player", message);
        }

        /// <inheritdoc />
        public void DisplayMessageOnSelf(string author, string message, string font)
        {
            MyHud.Chat.ShowMessage(author, message, font);
            MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat.Enqueue(new MyGlobalChatItem()
            {
                Author = author,
                AuthorFont = font,
                Text = message
            });
        }

        /// <inheritdoc/>
        public override void Attach()
        {
            base.Attach();
            if (MyMultiplayer.Static == null)
                _log.Warn("Currently ChatManagerClient doesn't support handling on an offline client");
            else
            {
                _chatMessageRecievedReplacer = _chatMessageReceivedFactory.Invoke();
                _scriptedChatMessageRecievedReplacer = _scriptedChatMessageReceivedFactory.Invoke();
                _chatMessageRecievedReplacer.Replace(new Action<ulong, string>(Multiplayer_ChatMessageReceived),
                    MyMultiplayer.Static);
                _scriptedChatMessageRecievedReplacer.Replace(
                    new Action<string, string, string>(Multiplayer_ScriptedChatMessageReceived), MyMultiplayer.Static);
            }
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            if (_chatMessageRecievedReplacer != null && _chatMessageRecievedReplacer.Replaced)
                _chatMessageRecievedReplacer.Restore(MyHud.Chat);
            if (_scriptedChatMessageRecievedReplacer != null && _scriptedChatMessageRecievedReplacer.Replaced)
                _scriptedChatMessageRecievedReplacer.Restore(MyHud.Chat);
            base.Detach();
        }

        private void Multiplayer_ChatMessageReceived(ulong steamUserId, string message)
        {
            var torchMsg = new TorchChatMessage()
            {
                AuthorSteamId = steamUserId,
                Author = Torch.CurrentSession.Managers.GetManager<IMultiplayerManagerBase>()
                              ?.GetSteamUsername(steamUserId),
                Font = (steamUserId == MyGameService.UserId) ? "DarkBlue" : "Blue",
                Message = message
            };
            var consumed = false;
            MessageRecieved?.Invoke(torchMsg, ref consumed);
            if (!consumed)
                _hudChatMessageReceived.Invoke(MyHud.Chat, steamUserId, message);
        }

        private void Multiplayer_ScriptedChatMessageReceived(string author, string message, string font)
        {
            var torchMsg = new TorchChatMessage()
            {
                AuthorSteamId = null,
                Author = author,
                Font = font,
                Message = message
            };
            var consumed = false;
            MessageRecieved?.Invoke(torchMsg, ref consumed);
            if (!consumed)
                _hudChatScriptedMessageReceived.Invoke(MyHud.Chat, author, message, font);
        }

        private const string _hudChatMessageReceivedName = "Multiplayer_ChatMessageReceived";
        private const string _hudChatScriptedMessageReceivedName = "multiplayer_ScriptedChatMessageReceived";

        [ReflectedMethod(Name = _hudChatMessageReceivedName)]
        private static Action<MyHudChat, ulong, string> _hudChatMessageReceived;
        [ReflectedMethod(Name = _hudChatScriptedMessageReceivedName)]
        private static Action<MyHudChat, string, string, string> _hudChatScriptedMessageReceived;

        [ReflectedEventReplace(typeof(MyMultiplayerBase), nameof(MyMultiplayerBase.ChatMessageReceived), typeof(MyHudChat), _hudChatMessageReceivedName)]
        private static Func<ReflectedEventReplacer> _chatMessageReceivedFactory;
        [ReflectedEventReplace(typeof(MyMultiplayerBase), nameof(MyMultiplayerBase.ScriptedChatMessageReceived), typeof(MyHudChat), _hudChatScriptedMessageReceivedName)]
        private static Func<ReflectedEventReplacer> _scriptedChatMessageReceivedFactory;

        private ReflectedEventReplacer _chatMessageRecievedReplacer;
        private ReflectedEventReplacer _scriptedChatMessageRecievedReplacer;
    }
}
