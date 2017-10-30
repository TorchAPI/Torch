using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using VRage.Game;

namespace Torch.Managers.Profiler
{
    internal static class ProfilerObjectIdentifier
    {
        /// <summary>
        /// Identifies the given object in a human readable name when profiling
        /// </summary>
        /// <param name="o">object to ID</param>
        /// <returns>ID</returns>
        public static string Identify(object o)
        {
            if (o is MyCubeGrid grid)
            {
                string owners = string.Join(", ", grid.BigOwners.Concat(grid.SmallOwners).Distinct().Select(
                    x => Sync.Players?.TryGetIdentity(x)?.DisplayName ?? $"Identity[{x}]"));
                if (string.IsNullOrWhiteSpace(owners))
                    owners = "unknown";
                return $"{grid.DisplayName ?? ($"{grid.GridSizeEnum} {grid.EntityId}")} owned by [{owners}]";
            }
            if (o is MyDefinitionBase def)
            {
                string typeIdSimple = def.Id.TypeId.ToString().Substring("MyObjectBuilder_".Length);
                string subtype = def.Id.SubtypeName?.Replace(typeIdSimple, "");
                return string.IsNullOrWhiteSpace(subtype) ? typeIdSimple : $"{typeIdSimple}::{subtype}";
            }
            if (o is string str)
            {
                return !string.IsNullOrWhiteSpace(str) ? str : "unknown string";
            }
            if (o is ProfilerFixedEntry fx)
            {
                string res = fx.ToString();
                return !string.IsNullOrWhiteSpace(res) ? res : "unknown fixed";
            }
            return o?.GetType().Name ?? "unknown";
        }
    }
}
