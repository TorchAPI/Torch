using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    /// Represents a try/catch block operation type
    /// </summary>
    public enum MsilTryCatchOperationType
    {
        // TryCatchBlockIL:
        // var exBlock = ILGenerator.BeginExceptionBlock();
        // try{
        // ILGenerator.BeginCatchBlock(typeof(Exception));
        // } catch(Exception e) {
        // ILGenerator.BeginCatchBlock(null);
        // } catch {
        // ILGenerator.BeginFinallyBlock();
        // }finally {
        // ILGenerator.EndExceptionBlock();
        // }
        BeginExceptionBlock,
        BeginCatchBlock,
        BeginFinallyBlock,
        EndExceptionBlock
    }

    /// <summary>
    /// Represents a try catch operation.
    /// </summary>
    public class MsilTryCatchOperation
    {
        /// <summary>
        /// Operation type
        /// </summary>
        public readonly MsilTryCatchOperationType Type;
        /// <summary>
        /// Type caught by this operation, or null if none.
        /// </summary>
        public readonly Type CatchType;

        public MsilTryCatchOperation(MsilTryCatchOperationType op, Type caughtType = null)
        {
            Type = op;
            if (caughtType != null && op != MsilTryCatchOperationType.BeginCatchBlock)
                throw new ArgumentException($"Can't use caught type with operation type {op}", nameof(caughtType));
            CatchType = caughtType;
        }
    }
}
