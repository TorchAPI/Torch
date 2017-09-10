using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents the operand for an inline switch statement
    /// </summary>
    public class MsilOperandSwitch : MsilOperand
    {
        internal MsilOperandSwitch(MsilInstruction instruction) : base(instruction)
        {
        }

        /// <summary>
        ///     The target labels for this switch
        /// </summary>
        public MsilLabel[] Labels { get; set; }

        internal override void Read(MethodContext context, BinaryReader reader)
        {
            int length = reader.ReadInt32();
            int offset = (int) reader.BaseStream.Position + 4 * length;
            Labels = new MsilLabel[length];
            for (var i = 0; i < Labels.Length; i++)
                Labels[i] = context.LabelAt(offset + reader.ReadInt32());
        }

        internal override void Emit(LoggingIlGenerator generator)
        {
            generator.Emit(Instruction.OpCode, Labels?.Select(x => x.LabelFor(generator))?.ToArray() ?? new Label[0]);
        }
    }
}