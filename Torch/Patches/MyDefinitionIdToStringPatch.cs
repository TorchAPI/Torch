// Caching ported from Performance Improvements 1.10.5
// Reasons:
// https://support.keenswh.com/spaceengineers/pc/topic/27997-servers-deadlocked-on-load
// https://support.keenswh.com/spaceengineers/pc/topic/24210-performance-pre-calculate-or-cache-mydefinitionid-tostring-results

#if DEBUG
// Uncomment this to enable the verification that all formatting actually matches the first one (effectively disables caching)
// #define VERIFY_RESULT
#endif

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using VRage.Game;
using Torch.Managers.PatchManager;
using Torch.Utils.Cache;

namespace Torch.Patches
{
    [PatchShim]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class MyDefinitionIdToStringPatch
    {
        private static bool enabled = true;

        public static bool Enabled
        {
            get => Enabled;
            set
            {
                enabled = value;
                if (!enabled)
                    Cache.Clear();
            }
        }

        private static readonly CacheForever<long, string> Cache = new CacheForever<long, string>();
        private static long nextFill;
        private const long FillPeriod = 37 * 60;  // frames

#if DEBUG
        private static string CacheReport => $"{Cache.ImmutableReport} | {Cache.Report}";
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
#endif

        public static void Update(long uptime)
        {
            if (!enabled || uptime < nextFill)
                return;

            nextFill = uptime + FillPeriod;
        
#if DEBUG
            _log.Info($"{nameof(MyDefinitionIdToStringPatch)}: {CacheReport}");
#endif

            Cache.FillImmutableCache();
        }
        
        // ReSharper disable once UnusedMember.Global
        internal static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MyDefinitionId).GetMethod(nameof(MyDefinitionId.ToString))).Prefixes.Add(typeof(MyDefinitionIdToStringPatch).GetMethod(nameof(MyDefinitionIdToStringPrefix), BindingFlags.Static | BindingFlags.NonPublic));
        }
        
        // ReSharper disable once UnusedMember.Local
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MyDefinitionIdToStringPrefix(MyDefinitionId __instance, ref string __result)
        {
            if (!enabled)
                return true;

            if (Cache.TryGetValue(__instance.GetHashCodeLong(), out __result))
            {
#if DEBUG && VERIFY_RESULT
                var expectedName = Format(__instance);
                Debug.Assert(__result == expectedName);
#endif
                return false;
            }

            var result = Format(__instance);
            Cache.Store(__instance.GetHashCodeLong(), result);

            __result = result;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Format(MyDefinitionId definitionId)
        {
            const string DefinitionNull = "(null)";

            var typeId = definitionId.TypeId;
            var typeName = typeId.IsNull ? DefinitionNull : typeId.ToString();

            var subtypeName = definitionId.SubtypeName;
            if (string.IsNullOrEmpty(subtypeName))
                subtypeName = DefinitionNull;

            var sb = ObjectPools.StringBuilder.Get(typeName.Length + 1 + subtypeName.Length);
            var text = sb.Append(typeName).Append('/').Append(subtypeName).ToString();
            ObjectPools.StringBuilder.Return(sb);

            return text;
        }
    }
}