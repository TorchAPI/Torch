using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace Torch.Managers
{
    public class ChatManager
    {
        public ChatManager()
        {
            NetworkManager.Instance.RegisterNetworkHandlers(new ChatIntercept());
        }

        private static ChatManager _instance;
        public static ChatManager Instance => _instance ?? (_instance = new ChatManager());

        public delegate void MessageRecievedDel(ChatMsg msg, ref bool sendToOthers);

        public event MessageRecievedDel MessageRecieved;

        internal void RaiseMessageRecieved(ChatMsg msg, ref bool sendToOthers) =>
            MessageRecieved?.Invoke(msg, ref sendToOthers);
    }

    internal class ChatIntercept : NetworkHandlerBase
    {
        private bool? _unitTestResult;
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

        public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj)
        {
            var msg = new ChatMsg();

            base.Serialize(site.MethodInfo, stream, ref msg);

            bool sendToOthers = true;
            ChatManager.Instance.RaiseMessageRecieved(msg, ref sendToOthers);

            return !sendToOthers;
        }
    }
}
