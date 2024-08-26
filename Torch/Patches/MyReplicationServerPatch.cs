using NLog;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Network;

namespace Torch.Patches
{
    [PatchShim]
    public static class MyReplicationServerPatch
    {
        public static Logger Log = LogManager.GetCurrentClassLogger();
        
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyReplicationServer).GetMethod("OnClientConnected", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                .Suffixes.Add(typeof(MyReplicationServerPatch).GetMethod(nameof(OnClientConnectedSuffix), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
        }
        
        public static void OnClientConnectedSuffix(ref ConnectedClientDataMsg __result, MyPacket packet)
        {
            int nameLimit = 32;

            if (__result.Name.Length > nameLimit) 
            {
                __result.Name = __result.Name.Substring(0, nameLimit);
            }
        }
    }
}