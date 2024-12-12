using System;
using System.Reflection;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    /// Represents metadata about a method's parameter
    /// </summary>
    public class MsilArgument
    {
        /// <summary>
        /// The positon of this argument.  Note, if the method is static, index 0 is the instance.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The type of this parameter, or null if unknown.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The name of this parameter, or null if unknown.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates an argument from the given parameter info.
        /// </summary>
        /// <param name="local">parameter info to use</param>
        public MsilArgument(ParameterInfo local)
        {
            bool isStatic;
            if (local.Member is FieldInfo fi)
                isStatic = fi.IsStatic;
            else if (local.Member is MethodBase mb)
                isStatic = mb.IsStatic;
            else
                throw new ArgumentException("ParameterInfo.Member must be MethodBase or FieldInfo", nameof(local));
            Position = (isStatic ? 0 : 1) + local.Position;
            Type = local.ParameterType;
            Name = local.Name;
        }

        /// <summary>
        /// Creates an empty argument reference with the given position.
        /// </summary>
        /// <param name="position">The argument's position</param>
        public MsilArgument(int position)
        {
            Position = position;
            Type = null;
            Name = null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"arg{Position:X4}({Type?.Name ?? "unknown"})";
        }
    }
}
