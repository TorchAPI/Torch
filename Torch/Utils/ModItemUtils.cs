using System;
using Sandbox.Engine.Networking;
using VRage.Game;

namespace Torch.Utils
{
    public static class ModItemUtils
    {
        /// <summary>
        /// Creates <see cref="MyObjectBuilder_Checkpoint.ModItem"/> from mod id and optionally service name.
        /// </summary>
        /// <param name="modId">Mod id.</param>
        /// <param name="serviceType">Service name, will be default if null passed.</param>
        /// <returns><see cref="MyObjectBuilder_Checkpoint.ModItem"/></returns>
        public static MyObjectBuilder_Checkpoint.ModItem Create(ulong modId, string serviceType = null)
        {
            return new MyObjectBuilder_Checkpoint.ModItem(modId, serviceType ?? GetDefaultServiceName());
        }

        /// <summary>
        /// Creates <see cref="MyObjectBuilder_Checkpoint.ModItem"/> from <see cref="string"/> containing mod id and the service name.
        /// </summary>
        /// <param name="str">String containing mod id and the service name.</param>
        /// <returns><see cref="MyObjectBuilder_Checkpoint.ModItem"/></returns>
        /// <exception cref="FormatException" />
        /// <exception cref="OverflowException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public static MyObjectBuilder_Checkpoint.ModItem Create(string str)
        {
            var arr = str.Split('-');
            return new MyObjectBuilder_Checkpoint.ModItem(ulong.Parse(arr[0]), arr[1]);
        }
        
        /// <summary>
        /// Tries to parse mod id <see cref="string"/> with or without the service name.
        /// </summary>
        /// <param name="str"><see cref="string"/> containing mod id and optionally the service name.</param>
        /// <param name="item">Parsed <see cref="MyObjectBuilder_Checkpoint.ModItem"/> or default if parsing was unsuccessful.</param>
        /// <returns>Parsing operation was successful or not.</returns>
        public static bool TryParse(string str, out MyObjectBuilder_Checkpoint.ModItem item)
        {
            item = default;
            
            var arr = str.Split('-');
            
            if (arr.Length == 0 || arr.Length > 2)
                return false;
            
            if (!ulong.TryParse(arr[0], out var id))
                return false;

            if (arr.Length == 1 || !TryParseServiceName(arr[1], out var serviceName))
                serviceName = GetDefaultServiceName();

            item = new MyObjectBuilder_Checkpoint.ModItem(id, serviceName);
            return true;
        }

        /// <summary>
        /// Tries to format service name to standardized and recognizable by game.
        /// </summary>
        /// <param name="str">Raw service name.</param>
        /// <param name="serviceName">Parsed service name or null if parsing was unsuccessful.</param>
        /// <returns>Parsing operation was successful or not.</returns>
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

        /// <summary>
        /// Default service name according to current configuration.
        /// </summary>
        /// <returns>UGC Service name.</returns>
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