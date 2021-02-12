using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Torch.API;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    internal static class ModsDownloadingPatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MyWorkshop), nameof(MyWorkshop.DownloadWorldModsBlocking))]
        private readonly static MethodInfo _downloadWorldModsBlockingMethod;
#pragma warning restore 649

        public static void Patch(PatchContext ctx)
        {
            _log.Info("Patching mods downloading");
            ctx.GetPattern(_downloadWorldModsBlockingMethod).Suffixes
                .Add(typeof(ModsDownloadingPatch).GetMethod(nameof(Postfix)));
            var pattern = ctx.GetPattern(_downloadWorldModsBlockingMethod);
        }

        
        public static void Postfix(MyWorkshop.ResultData __result)
        {
            if (__result.Success) return;
            _log.Warn("Missing Mods:");
            __result.MismatchMods.ForEach(b => _log.Info($"\t{b.Title} : {b.Id}"));
        }
    }
}