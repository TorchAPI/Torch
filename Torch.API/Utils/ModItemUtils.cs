using Sandbox.Engine.Networking;
using VRage.Game;

namespace Torch.Utils
{
    public static class ModItemUtils
    {
        public static MyObjectBuilder_Checkpoint.ModItem Create(ulong modId)
        {
            var serviceName = "Steam";
            if (MyGameService.IsOnline)
                serviceName = MyGameService.GetDefaultUGC().ServiceName;
            return new MyObjectBuilder_Checkpoint.ModItem(modId, serviceName);
        }
    }
}