using System;
using System.Collections.Generic;
using System.Reflection;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.ModAPI;

namespace Torch.Patches;

internal static class EntityIdentifierPatch
{
    [ReflectedGetter(Type = typeof(MyEntityIdentifier), Name = "m_perThreadData")]
    private static Func<object> _perThreadGetter = null!;

    [ReflectedGetter(TypeName = "VRage.MyEntityIdentifier+PerThreadData, VRage.Game", Name = "EntityList")]
    private static Func<object, Dictionary<long, IMyEntity>> _entityDataGetter = null;

    [ReflectedMethodInfo(typeof(MyEntityIdentifier), "GetPerThreadEntities")]
    private static MethodInfo _getPerThreadMethod = null!;

    public static void Patch(PatchContext context)
    {
        context.GetPattern(_getPerThreadMethod).AddPrefix(nameof(GetPerThreadPrefix));
    }

    // Rider DPA
    // Large Object Heap: Allocated 286,3 MB (300156688 B) of type VRage.ModAPI.IMyEntity[] by
    // List<__Canon>.AddWithResize() -> MyEntityIdentifier.GetPerThreadEntities(List)
    private static bool GetPerThreadPrefix(List<IMyEntity> result)
    {
        /*
         * This is better than 100500 calls of .Add because .Values returns ICollection<>
         * .AddRange will work with it without additional enumerations
         */
        result.AddRange(_entityDataGetter(_perThreadGetter()).Values);
        return false;
    }
}