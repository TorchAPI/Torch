using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager.MSIL;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Functions that let you read and write MSIL to methods directly.
    /// </summary>
    public class PatchUtilities
    {
        /// <summary>
        /// Gets the content of a method as an instruction stream
        /// </summary>
        /// <param name="method">Method to examine</param>
        /// <returns>instruction stream</returns>
        public static IEnumerable<MsilInstruction> ReadInstructions(MethodBase method)
        {
            var context = new MethodContext(method);
            context.Read();
            context.CheckIntegrity();
            return context.Instructions;
        }

        /// <summary>
        /// Writes the given instruction stream to the given IL generator, fixing short branch instructions.
        /// </summary>
        /// <param name="insn">Instruction stream</param>
        /// <param name="generator">Output</param>
        public static void EmitInstructions(IEnumerable<MsilInstruction> insn, LoggingIlGenerator generator)
        {
            MethodTranspiler.Emit(insn, generator);
        }
    }
}
