using System.Reflection;
using NLog;
using Sandbox.Engine.Multiplayer;
using Torch.Managers.PatchManager;
using VRage.Network;

namespace Torch.Patches
{
    [PatchShim]
    internal static class MessageSizeLimitPatch
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        internal static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MyMultiplayerBase).GetMethod("OnChatMessageReceived_Server", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Prefixes.Add(typeof(MessageSizeLimitPatch).GetMethod(nameof(PrefixHandleChatMessage), BindingFlags.Static | BindingFlags.NonPublic));
            context.GetPattern(typeof(MyMultiplayerBase).GetMethod("OnScriptedChatMessageReceived", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Prefixes.Add(typeof(MessageSizeLimitPatch).GetMethod(nameof(PrefixHandleScriptedMessage), BindingFlags.Static | BindingFlags.NonPublic));
            _log.Info("Patched MessageSizeLimit");
        }
        private static bool PrefixHandleChatMessage(ChatMsg msg)
        {
            if (msg.Text.Length > 2048)
            {
                _log.Warn("Attempted message with character count greater than 2048");
                return false;
            }
            return true;
        }
        private static bool PrefixHandleScriptedMessage(ScriptedChatMsg msg)
        {
            if (msg.Text.Length > 2048)
            {
                _log.Warn("Attempted message with character count greater than 2048");
                return false;
            }
            return true;
        }
    }
}
