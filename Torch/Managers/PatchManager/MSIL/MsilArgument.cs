using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

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

        internal MsilArgument(ParameterInfo local)
        {
            Position = (((MethodBase)local.Member).IsStatic ? 0 : 1) + local.Position;
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
    }
}
