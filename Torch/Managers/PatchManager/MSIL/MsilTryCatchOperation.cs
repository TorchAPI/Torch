using System;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    /// Represents a try/catch block operation type
    /// </summary>
    public enum MsilTryCatchOperationType
    {
        BeginExceptionBlock,
        BeginClauseBlock,
        BeginFaultBlock,
        BeginFinallyBlock,
        EndExceptionBlock
    }

    /// <summary>
    /// Represents a try catch operation.
    /// </summary>
    public class MsilTryCatchOperation
    {
        internal int NativeOffset;
        
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
            if (caughtType != null && op != MsilTryCatchOperationType.BeginClauseBlock)
                throw new ArgumentException($"Can't use caught type with operation type {op}", nameof(caughtType));
            CatchType = caughtType;
        }

        public override string ToString() => $"{Type} -> {CatchType}";
    }
}
