using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;
using Torch.API;
using VRage.Game;

namespace Torch.Utils
{
    public static class ModItemUtils
    {
        public static MyObjectBuilder_Checkpoint.ModItem Create(ulong modId, string serviceType = null)
        {
            return new MyObjectBuilder_Checkpoint.ModItem(modId, serviceType ?? GetDefaultServiceName());
        }

        public static MyObjectBuilder_Checkpoint.ModItem Create(string str)
        {
            var arr = str.Split('-');
            return new MyObjectBuilder_Checkpoint.ModItem(ulong.Parse(arr[0]), arr[1]);
        }
        
        public static bool TryParse(string str, out MyObjectBuilder_Checkpoint.ModItem item)
        {
            item = default;
            
            var arr = str.Split('-');
            
            if (arr.Length is 0 or > 2)
                return false;
            
            if (!ulong.TryParse(arr[0], out var id))
                return false;

            if (arr.Length == 1 || !TryParseServiceName(arr[1], out var serviceName))
                serviceName = GetDefaultServiceName();

            item = new(id, serviceName);
            return true;
        }

        public static bool TryParseServiceName(string str, out string serviceName)
        {
            if (str.Equals("steam", StringComparison.OrdinalIgnoreCase))
            {
                serviceName = "Steam";
                return true;
            }
            if (str.Equals("mod.io", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("eos", StringComparison.OrdinalIgnoreCase))
            {
                serviceName = "mod.io";
                return true;
            }

            serviceName = null;
            return false;
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
#pragma warning disable CS0618
                return TorchBase.Instance.Config.UgcServiceType.ToString();
#pragma warning restore CS0618
            }
        }
    }
}