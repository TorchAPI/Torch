using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents a single MSIL instruction, and its operand
    /// </summary>
    public class MsilInstruction
    {
        private MsilOperand _operandBacking;

        /// <summary>
        ///     Creates a new instruction with the given opcode.
        /// </summary>
        /// <param name="opcode">Opcode</param>
        public MsilInstruction(OpCode opcode)
        {
            OpCode = opcode;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    Operand = null;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    Operand = new MsilOperandBrTarget(this);
                    break;
                case OperandType.InlineField:
                    Operand = new MsilOperandInline.MsilOperandReflected<FieldInfo>(this);
                    break;
                case OperandType.InlineI:
                    Operand = new MsilOperandInline.MsilOperandInt32(this);
                    break;
                case OperandType.InlineI8:
                    Operand = new MsilOperandInline.MsilOperandInt64(this);
                    break;
                case OperandType.InlineMethod:
                    Operand = new MsilOperandInline.MsilOperandReflected<MethodInfo>(this);
                    break;
                case OperandType.InlineR:
                    Operand = new MsilOperandInline.MsilOperandDouble(this);
                    break;
                case OperandType.InlineSig:
                    Operand = new MsilOperandInline.MsilOperandSignature(this);
                    break;
                case OperandType.InlineString:
                    Operand = new MsilOperandInline.MsilOperandString(this);
                    break;
                case OperandType.InlineSwitch:
                    Operand = new MsilOperandSwitch(this);
                    break;
                case OperandType.InlineTok:
                    Operand = new MsilOperandInline.MsilOperandReflected<MemberInfo>(this);
                    break;
                case OperandType.InlineType:
                    Operand = new MsilOperandInline.MsilOperandReflected<Type>(this);
                    break;
                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                    if (OpCode.Name.IndexOf("loc", StringComparison.OrdinalIgnoreCase) != -1)
                        Operand = new MsilOperandInline.MsilOperandLocal(this);
                    else
                        Operand = new MsilOperandInline.MsilOperandParameter(this);
                    break;
                case OperandType.ShortInlineI:
                    Operand = OpCode == OpCodes.Ldc_I4_S
                        ? (MsilOperand) new MsilOperandInline.MsilOperandInt8(this)
                        : new MsilOperandInline.MsilOperandUInt8(this);
                    break;
                case OperandType.ShortInlineR:
                    Operand = new MsilOperandInline.MsilOperandSingle(this);
                    break;
#pragma warning disable 618
                case OperandType.InlinePhi:
#pragma warning restore 618
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Opcode of this instruction
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        ///     Raw memory offset of this instruction; optional.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        ///     The operand for this instruction, or null.
        /// </summary>
        public MsilOperand Operand
        {
            get => _operandBacking;
            set
            {
                if (_operandBacking != null && value.GetType() != _operandBacking.GetType())
                    throw new ArgumentException($"Operand for {OpCode.Name} must be {_operandBacking.GetType().Name}");
                _operandBacking = value;
            }
        }

        /// <summary>
        ///     Labels pointing to this instruction.
        /// </summary>
        public HashSet<MsilLabel> Labels { get; } = new HashSet<MsilLabel>();

        /// <summary>
        ///     Sets the inline value for this instruction.
        /// </summary>
        /// <typeparam name="T">The type of the inline constraint</typeparam>
        /// <param name="o">Value</param>
        /// <returns>This instruction</returns>
        public MsilInstruction InlineValue<T>(T o)
        {
            ((MsilOperandInline<T>) Operand).Value = o;
            return this;
        }

        /// <summary>
        ///     Sets the inline branch target for this instruction.
        /// </summary>
        /// <param name="label">Target to jump to</param>
        /// <returns>This instruction</returns>
        public MsilInstruction InlineTarget(MsilLabel label)
        {
            ((MsilOperandBrTarget) Operand).Target = label;
            return this;
        }

        /// <summary>
        ///     Emits this instruction to the given generator
        /// </summary>
        /// <param name="target">Emit target</param>
        public void Emit(LoggingIlGenerator target)
        {
            foreach (MsilLabel label in Labels)
                target.MarkLabel(label.LabelFor(target));
            if (Operand != null)
                Operand.Emit(target);
            else
                target.Emit(OpCode);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (MsilLabel label in Labels)
                sb.Append(label).Append(": ");
            sb.Append(OpCode.Name).Append("\t").Append(Operand);
            return sb.ToString();
        }
    }
}