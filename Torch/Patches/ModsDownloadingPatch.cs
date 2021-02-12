using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    internal class ModsDownloadingPatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MyWorkshop), nameof(MyWorkshop.DownloadWorldModsBlocking))]
        private readonly static MethodInfo _downloadWorldModsBlockingMethod;
#pragma warning restore 649

        public static void Patch(PatchContext ctx)
        {
            _log.Info("Patching mods downloading");
            var pattern = ctx.GetPattern(_downloadWorldModsBlockingMethod);
            pattern.Suffixes
                .Add(typeof(ModsDownloadingPatch).GetMethod(nameof(Postfix)));
            pattern.Prefixes.Add(typeof(ModsDownloadingPatch).GetMethod(nameof(Prefix)));
        }

        public static void Prefix(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            var serviceName = MyGameService.GetDefaultUGC().ServiceName;
            mods = mods?.Select(b => { 
                b.PublishedServiceName = serviceName;
                return b;
            }).ToList();
        }
        
        public static void Postfix(MyWorkshop.ResultData __result)
        {
            if (!__result.Success)
            {
                _log.Warn("Missing Mods:");
                __result.MismatchMods.ForEach(b => _log.Info($"\t{b.Title} : {b.Id}"));
            }
        }
    }
}