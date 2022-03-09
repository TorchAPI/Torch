using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Torch.Patches
{
    [PatchShim]
    public static class FactionValidationPatch
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        internal static void Patch(PatchContext context) {
            context.GetPattern(typeof(MyFactionCollection).GetMethod("CreateFactionServer", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Prefixes.Add(typeof(FactionValidationPatch).GetMethod(nameof(PrefixCreateFactionServer), BindingFlags.Static | BindingFlags.NonPublic));

            _log.Info("Patched CreateFactionServer");
        }

        // ReSharper disable once InconsistentNaming
        private static bool PrefixCreateFactionServer(long founderId, string factionTag, string factionName, string description, string privateInfo,
            MyFactionDefinition factionDef = null, MyFactionTypes type = MyFactionTypes.None, SerializableDefinitionId? factionIconGroupId = null, int factionIconId = 0,
            Vector3 factionColor = new Vector3(), Vector3 factionIconColor = new Vector3(), int score = 0) {

            //regex to see if any string follows format of \&.*?\;
            Regex regex = new Regex(@"\&.*?\;");
            
            //replace null strings with empty strings
            privateInfo = privateInfo ?? "";
            description = description ?? "";
            factionName = factionName ?? "";
            factionTag = factionTag ?? "";
            
            
            if (regex.IsMatch(factionTag) || regex.IsMatch(factionName) || regex.IsMatch(description) || regex.IsMatch(privateInfo)) {
                _log.Warn($"Attempted creation of faction with illegal characters.");
                return false;
            }
            
            string pattern = "[^ -~]+";
            Regex reg_exp = new Regex(pattern);
            if (reg_exp.IsMatch(factionTag) || reg_exp.IsMatch(factionName) || reg_exp.IsMatch(description) || reg_exp.IsMatch(privateInfo))
            {
                return false;
            }

            //check to see if any of the strings are longer than 512 characters
            if (factionTag.Length > 512 || factionName.Length > 512 || description.Length > 512 || privateInfo.Length > 512) {
                _log.Warn($"Attempted creation of faction with illegal string lengths.");
                return false;
            }
            
            return true;
        }
    }
}