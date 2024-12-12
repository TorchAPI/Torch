using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    /// Represents metadata about a method's local
    /// </summary>
    public class MsilLocal
    {
        /// <summary>
        /// The index of this local.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The type of this local, or null if unknown.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The name of this local, or null if unknown.
        /// </summary>
        public string Name { get; }

        internal MsilLocal(LocalBuilder local)
        {
            Index = local.LocalIndex;
            Type = local.LocalType;
            Name = null;
        }

        internal MsilLocal(LocalVariableInfo local)
        {
            Index = local.LocalIndex;
            Type = local.LocalType;
            Name = null;
        }

        /// <summary>
        /// Creates an empty local reference with the given index.
        /// </summary>
        /// <param name="index">The local's index</param>
        public MsilLocal(int index)
        {
            Index = index;
            Type = null;
            Name = null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"lcl{Index:X4}({Type?.Name ?? "unknown"})";
        }
    }
}
