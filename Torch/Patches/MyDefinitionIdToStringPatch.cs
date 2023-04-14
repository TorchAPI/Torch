// Caching ported from Performance Improvements 1.10.5
// Reasons:
// https://support.keenswh.com/spaceengineers/pc/topic/27997-servers-deadlocked-on-load
// https://support.keenswh.com/spaceengineers/pc/topic/24210-performance-pre-calculate-or-cache-mydefinitionid-tostring-results

// Uncomment this to enable the verification that all formatting actually matches the first one (effectively disables caching)
// #define VERIFY_RESULT

// Uncomment to enable logging statistics
#define LOG_STATS

using System;
using System.Diagnostics;
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
        private static readonly CacheForever<long, string> Cache = new CacheForever<long, string>();
        private static long nextFill;
        private const long FillPeriod = 37 * 60; // frames

#if LOG_STATS
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static string CacheReport => $"L1-RO: {Cache.ImmutableReport} | L2-RW: {Cache.Report}";
#endif

        public static void Update(long ticks)
        {
            if (ticks < nextFill)
                return;

            nextFill = ticks + FillPeriod;

#if LOG_STATS
            _log.Info($"{nameof(MyDefinitionIdToStringPatch)}: {CacheReport}");
#endif

            Cache.FillImmutableCache();
        }

        // ReSharper disable once UnusedMember.Global
        internal static void Patch(PatchContext context)
        {
            var targetMethod = typeof(MyDefinitionId).GetMethod(nameof(MyDefinitionId.ToString), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            Debug.Assert(targetMethod != null);

            var patchMethod = typeof(MyDefinitionIdToStringPatch).GetMethod(nameof(ToStringPrefix), BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(patchMethod != null);

            context.GetPattern(targetMethod).Prefixes.Add(patchMethod);
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once RedundantAssignment
        private static bool ToStringPrefix(MyDefinitionId __instance, ref string __result)
        {
            try
            {
                if (Cache.TryGetValue(__instance.GetHashCodeLong(), out __result))
                {
#if VERIFY_RESULT
                var expectedName = Format(__instance);
                Debug.Assert(__result == expectedName);
#endif
                    return false;
                }

                var result = Format(__instance);
                Cache.Store(__instance.GetHashCodeLong(), result);

                __result = result;
            }
            catch (Exception e)
            {
                _log.Error($"{e.Message}: {e}");
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Format(MyDefinitionId definitionId)
        {
#if false
            // Original code
            string typeId = !definitionId.TypeId.IsNull ? definitionId.TypeId.ToString() : "(null)";
            string subtypeName = !string.IsNullOrEmpty(definitionId.SubtypeName) ? definitionId.SubtypeName : "(null)";
            return string.Format("{0}/{1}", typeId, subtypeName);
#endif

            // Same with less memory allocation due to reusing StringBuilder instances
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