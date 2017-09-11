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
            int val = Instruction.OpCode.OperandType == OperandType.InlineBrTarget
                ? reader.ReadInt32()
                : reader.ReadSByte();
            Target = context.LabelAt((int)reader.BaseStream.Position + val);
        }

        internal override void Emit(LoggingIlGenerator generator)
        {
            generator.Emit(Instruction.OpCode, Target.LabelFor(generator));
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