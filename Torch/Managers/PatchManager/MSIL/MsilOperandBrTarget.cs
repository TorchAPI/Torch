using System;
using System.IO;
using System.Reflection.Emit;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents a branch target operand.
    /// </summary>
    public class MsilOperandBrTarget : MsilOperand
    {
        internal MsilOperandBrTarget(MsilInstruction instruction) : base(instruction)
        {
        }

        /// <summary>
        ///     Branch target
        /// </summary>
        public MsilLabel Target { get; set; }

        internal override void Read(MethodContext context, BinaryReader reader)
        {

            long offset;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (Instruction.OpCode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                    offset = reader.ReadSByte();
                    break;
                case OperandType.InlineBrTarget:
                    offset = reader.ReadInt32();
                    break;
                default:
                    throw new InvalidBranchException(
                        $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
            }

            Target = context.LabelAt((int)(reader.BaseStream.Position + offset));
        }

        internal override void Emit(LoggingIlGenerator generator)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (Instruction.OpCode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    generator.Emit(Instruction.OpCode, Target.LabelFor(generator));
                    break;
                default:
                    throw new InvalidBranchException(
                        $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
            }

        }

        internal override void CopyTo(MsilOperand operand)
        {
            var lt = operand as MsilOperandBrTarget;
            if (lt == null)
                throw new ArgumentException($"Target {operand?.GetType().Name} must be of same type {GetType().Name}", nameof(operand));
            lt.Target = Target;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Target?.ToString() ?? "null";
        }
    }
}