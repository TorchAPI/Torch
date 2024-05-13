using Torch.Managers.PatchManager;
using System.Reflection;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace Torch.Patches
{
    [PatchShim]
    public static class MyRespawnComponentPatch
    {
        public static Logger _log = LogManager.GetCurrentClassLogger();
        public static void Patch(PatchContext ctx)
        {
            var target = typeof(MyRespawnComponent).GetMethod("CanPlayerSpawn", BindingFlags.Instance | BindingFlags.Public);
            var patch = typeof(MyRespawnComponentPatch).GetMethod(nameof(PrefixCanPlayerSpawn), BindingFlags.Static | BindingFlags.Public);
            
            ctx.GetPattern(target).Prefixes.Add(patch);
            _log.Info("Patching MyRespawnComponent.CanPlayerSpawn");
        }
        
        public static bool PrefixCanPlayerSpawn(MyRespawnComponent __instance, long playerId, bool acceptPublicRespawn, ref bool __result)
        {
            var block = __instance.Entity;
            if(block.HasPlayerAccess(playerId))
            {
                var idModule = block.IDModule;
                var relation = MyIDModule.GetRelationPlayerBlock(idModule.Owner, playerId, idModule.ShareMode, defaultShareWithAllRelations: MyRelationsBetweenPlayerAndBlock.Enemies);
                if(relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    __result = true;
                    return false;
                }
            }

            if(acceptPublicRespawn)
            {
                var medBay = block as MyMedicalRoom;
                if(medBay != null && medBay.SetFactionToSpawnee)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }
}