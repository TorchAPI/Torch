using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.API.Session;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using Torch.Mod;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    internal static class SessionDownloadPatch
    {
        private static ITorchSessionManager _sessionManager;
        private static ITorchSessionManager SessionManager => _sessionManager ?? (_sessionManager = TorchBase.Instance.Managers.GetManager<ITorchSessionManager>());


        internal static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MySession).GetMethod(nameof(MySession.GetWorld))).Suffixes.Add(typeof(SessionDownloadPatch).GetMethod(nameof(SuffixGetWorld), BindingFlags.Static | BindingFlags.NonPublic));
        }

        // ReSharper disable once InconsistentNaming
        private static void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            //copy this list so mods added here don't propagate up to the real session
            __result.Checkpoint.Mods = __result.Checkpoint.Mods.ToList();
            
            __result.Checkpoint.Mods.AddRange(SessionManager.OverrideMods);

            var factionsToRemove = new List<MyObjectBuilder_Faction>();
            foreach(var faction in __result.Checkpoint.Factions.Factions) {
                
                //replace null strings with empty strings
                string privateInfo = faction.PrivateInfo ?? "";
                string description = faction.Description ?? "";
                string factionName = faction.Name ?? "";
                string factionTag = faction.Tag ?? "";

                string pattern = "[^ -~]+";
                Regex reg_exp = new Regex(pattern);

                if (reg_exp.IsMatch(factionTag) || reg_exp.IsMatch(factionName) || reg_exp.IsMatch(description) || reg_exp.IsMatch(privateInfo)) {
                    faction.PrivateInfo = reg_exp.Replace(privateInfo, "_");
                    faction.Description = reg_exp.Replace(description, "_");
                    faction.Name = reg_exp.Replace(factionName, "_");
                    faction.Tag = reg_exp.Replace(factionTag, "_");
                    factionsToRemove.Add(faction);
                    continue;
                }
            }
            
            foreach (var faction in factionsToRemove) {
                __result.Checkpoint.Factions.Factions.Remove(faction);
                __result.Checkpoint.Factions.Factions.Add(faction);
            }
        }
    }
}
