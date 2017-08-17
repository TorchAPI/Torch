using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using VRage;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Serialization;
using VRage.Utils;

namespace Torch.Managers
{
    [Manager]
    public class ChatManager : Manager
    {
        private static Logger _log = LogManager.GetLogger(nameof(ChatManager));

        public delegate void MessageRecievedDel(ChatMsg msg, ref bool sendToOthers);

        public event MessageRecievedDel MessageRecieved;

        internal void RaiseMessageRecieved(ChatMsg msg, ref bool sendToOthers) =>
            MessageRecieved?.Invoke(msg, ref sendToOthers);

        public ChatManager(ITorchBase torchInstance) : base(torchInstance)
        {
            
        }

        public override void Init()
        {
            try
            {
                Torch.GetManager<INetworkManager>().RegisterNetworkHandler(new ChatIntercept(this));
            }
            catch
            {
                _log.Error("Failed to initialize network intercept, command hiding will not work! Falling back to another method.");
                MyMultiplayer.Static.ChatMessageReceived += Static_ChatMessageReceived;
            }
        }

        private void Static_ChatMessageReceived(ulong arg1, string arg2)
        {
            var msg = new ChatMsg {Author = arg1, Text = arg2};
            var sendToOthers = true;

            RaiseMessageRecieved(msg, ref sendToOthers);
        }

        internal class ChatIntercept : NetworkHandlerBase, INetworkHandler
        {
            private ChatManager _chatManager;
            private bool? _unitTestResult;

            public ChatIntercept(ChatManager chatManager)
            {
                _chatManager = chatManager;
            }

            public override bool CanHandle(CallSite site)
            {
                if (site.MethodInfo.Name != "OnChatMessageRecieved")
                    return false;

                if (_unitTestResult.HasValue)
                    return _unitTestResult.Value;

                var parameters = site.MethodInfo.GetParameters();
                if (parameters.Length != 1)
                {
                    _unitTestResult = false;
                    return false;
                }

                if (parameters[0].ParameterType != typeof(ChatMsg))
                    _unitTestResult = false;

                _unitTestResult = true;

                return _unitTestResult.Value;
            }

            public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj, MyPacket packet)
            {
                var msg = new ChatMsg();
                Serialize(site.MethodInfo, stream, ref msg);

                bool sendToOthers = true;
                _chatManager.RaiseMessageRecieved(msg, ref sendToOthers);

                return !sendToOthers;
            }
        }
    }
}
