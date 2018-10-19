using System.IO;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents an operand for a MSIL instruction
    /// </summary>
    public abstract class MsilOperand
    {
        protected MsilOperand(MsilInstruction instruction)
        {
            Instruction = instruction;
        }

        /// <summary>
        ///     Instruction this operand is associated with
        /// </summary>
        public MsilInstruction Instruction { get; }

        /// <summary>
        /// Gets the maximum amount of space this operand will use.
        /// </summary>
        public abstract int MaxBytes { get; }

        internal abstract void CopyTo(MsilOperand operand);

        internal abstract void Read(MethodContext context, BinaryReader reader);

        internal abstract void Emit(LoggingIlGenerator generator);
    }
}