using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Torch.Managers.PatchManager.Transpile;
using OperandType = System.Reflection.Emit.OperandType;

namespace Torch.Managers.PatchManager.MSIL;

internal static class InstructionExtensions
{
    public static MsilInstruction ToMsilInstruction(this Instruction instruction)
    {
        static System.Reflection.Emit.Label CreateLabel(int pos)
        {
            var instance = Activator.CreateInstance(typeof(System.Reflection.Emit.Label),
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] {pos}, null);
            if (instance == null)
                return default;
            return (System.Reflection.Emit.Label) instance;
        }

        var systemOpCode = MethodContext.OpCodeLookup[instruction.OpCode.Value];
        var msil = new MsilInstruction(systemOpCode);
        if (instruction.Operand is null || instruction.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineNone)
            return msil;

        var opType = systemOpCode.OperandType;
        
        switch (instruction.Operand)
        {
            case Instruction targetInstruction when opType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget:
                msil.Operand = new MsilOperandBrTarget(msil)
                {
                    Target = new MsilLabel(CreateLabel(targetInstruction.Offset))
                };
                break;
            case FieldReference reference when opType == OperandType.InlineField:
                msil.Operand = new MsilOperandInline.MsilOperandReflected<FieldInfo>(msil)
                {
                    Value = reference.ResolveReflection()
                };
                break;
            case int int32 when opType is OperandType.InlineI or OperandType.ShortInlineI:
                msil.Operand = new MsilOperandInline.MsilOperandInt32(msil)
                {
                    Value = int32
                };
                break;
            case long int64 when opType is OperandType.InlineI8:
                msil.Operand = new MsilOperandInline.MsilOperandInt64(msil)
                {
                    Value = int64
                };
                break;
            case MethodReference methodReference when opType is OperandType.InlineMethod:
                msil.Operand = new MsilOperandInline.MsilOperandReflected<MethodBase>(msil)
                {
                    Value = methodReference.ResolveReflection()
                };
                break;
            case double @double when opType is OperandType.InlineR:
                msil.Operand = new MsilOperandInline.MsilOperandDouble(msil)
                {
                    Value = @double
                };
                break;
            case null when opType is OperandType.InlineSig:
                throw new NotSupportedException("InlineSignature is not supported by instruction converter");
            case string @string when opType == OperandType.InlineString:
                msil.Operand = new MsilOperandInline.MsilOperandString(msil)
                {
                    Value = @string
                };
                break;
            case Instruction[] targetInstructions when opType is OperandType.InlineSwitch:
                msil.Operand = new MsilOperandSwitch(msil)
                {
                    Labels = targetInstructions.Select(b => new MsilLabel(CreateLabel(b.Offset))).ToArray()
                };
                break;
            case MemberReference memberReference when opType is OperandType.InlineTok:
                msil.Operand = new MsilOperandInline.MsilOperandReflected<MemberInfo>(msil)
                {
                    Value = memberReference.ResolveReflection()
                };
                break;
            case TypeReference typeReference when opType is OperandType.InlineType:
                msil.Operand = new MsilOperandInline.MsilOperandReflected<Type>(msil)
                {
                    Value = typeReference.ResolveReflection()
                };
                break;
            case VariableDefinition variableDefinition when opType is OperandType.InlineVar or OperandType.ShortInlineVar:
                if (systemOpCode.IsLocalStore() || systemOpCode.IsLocalLoad() || systemOpCode.IsLocalLoadByRef())
                    msil.Operand = new MsilOperandInline.MsilOperandLocal(msil)
                    {
                        Value = new MsilLocal(variableDefinition.Index)
                    };
                else
                    msil.Operand = new MsilOperandInline.MsilOperandArgument(msil)
                    {
                        Value = new MsilArgument(variableDefinition.Index)
                    };
                break;
            case ParameterDefinition parameterDefinition when opType is OperandType.InlineVar or OperandType.ShortInlineVar:
                if (systemOpCode.IsLocalStore() || systemOpCode.IsLocalLoad() || systemOpCode.IsLocalLoadByRef())
                    msil.Operand = new MsilOperandInline.MsilOperandLocal(msil)
                    {
                        Value = new MsilLocal(parameterDefinition.Index)
                    };
                else
                    msil.Operand = new MsilOperandInline.MsilOperandArgument(msil)
                    {
                        Value = new MsilArgument(parameterDefinition.Index)
                    };
                break;
            case float @float when opType == OperandType.ShortInlineR:
                msil.Operand = new MsilOperandInline.MsilOperandSingle(msil)
                {
                    Value = @float
                };
                break;
#pragma warning disable 618
            case null when opType == OperandType.InlinePhi:
#pragma warning restore 618
            default:
                throw new ArgumentOutOfRangeException(nameof(instruction.Operand), instruction.Operand, "Invalid operand type");
        }

        return msil;
    }
}