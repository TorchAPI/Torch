using Sandbox.Engine.Networking;
using VRage.Game;

namespace Torch.Utils
{
    public static class ModItemUtils
    {
        public static MyObjectBuilder_Checkpoint.ModItem Create(ulong modId)
        {
            return new MyObjectBuilder_Checkpoint.ModItem(modId, GetDefaultServiceName());
        }

        //because KEEEN! 
        public static string GetDefaultServiceName()
        {
            try
            {
                return MyGameService.GetDefaultUGC().ServiceName;
            }
            catch
            {
                return TorchBase.Instance.Config.UgcServiceType.ToString();
            }
        }
    }
}