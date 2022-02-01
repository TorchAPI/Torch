using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Havok;
using Sandbox.Engine.Voxels.Planet;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage;

namespace Torch.Patches;

[PatchShim]
internal static class GcCollectPatch
{
    // FUCK YO KEEN
    // every call results in freeze for seconds
    private static readonly MethodBase[] _targets =
    {
        Info.OfMethod<MyPlanetTextureMapProvider>(nameof(MyPlanetTextureMapProvider.GetHeightmap)),
        Info.OfMethod<MyPlanetTextureMapProvider>(nameof(MyPlanetTextureMapProvider.GetDetailMap)),
        Info.OfMethod<MyPlanetTextureMapProvider>(nameof(MyPlanetTextureMapProvider.GetMaps)),
        Info.OfMethod<MySession>(nameof(MySession.Unload)),
        Info.OfMethod("HavokWrapper", "Havok.HkBaseSystem", nameof(HkBaseSystem.Quit)),
        Info.OfMethod<MySimpleProfiler>(nameof(MySimpleProfiler.LogPerformanceTestResults)),
        Info.OfConstructor<MySession>("MySyncLayer,Boolean")
    };

    public static void Patch(PatchContext context)
    {
        foreach (var target in _targets)
        {
            context.GetPattern(target).AddTranspiler(nameof(CollectRemovalTranspiler));
        }
    }

    private static IEnumerable<MsilInstruction> CollectRemovalTranspiler(IEnumerable<MsilInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Operand is MsilOperandInline<MethodInfo> operand &&
                operand.Value.DeclaringType == typeof(GC))
            {
                yield return instruction.CopyWithoutOperand(OpCodes.Nop);
                continue;
            }
            yield return instruction;
        }
    }
}