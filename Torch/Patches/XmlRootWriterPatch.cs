using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage;

namespace Torch.Patches;

internal static class XmlRootWriterPatch
{
    [ReflectedMethodInfo(typeof(CustomRootWriter), "Init")]
    private static MethodInfo InitMethod = null!;
        
    public static void Patch(PatchContext context)
    {
        context.GetPattern(InitMethod).AddTranspiler();
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        var ins = instructions.ToList();
        var index = ins.FindIndex(b =>
            b.OpCode == OpCodes.Ldstr && b.Operand is MsilOperandInline.MsilOperandString {Value: "xsi:type"});
        ((MsilOperandInline.MsilOperandString)ins[index].Operand).Value = "xsi";
        ins.InsertRange(index + 1, new[]
        {
            new MsilInstruction(OpCodes.Ldstr).InlineValue("type"),
            new MsilInstruction(OpCodes.Ldstr).InlineValue("http://www.w3.org/2001/XMLSchema-instance")
        });
        var instruction = ins[ins.FindIndex(b => b.OpCode == OpCodes.Callvirt)];
        instruction.InlineValue(typeof(XmlWriter).GetMethod(
            "WriteAttributeString", new[]
            {
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            }));
        return ins;
    }
}