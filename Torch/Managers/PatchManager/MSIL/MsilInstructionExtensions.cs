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
    /// Various methods to make composing MSIL easier
    /// </summary>
    public static class MsilInstructionExtensions
    {
        #region Local Utils
        /// <summary>
        /// Is this instruction a local load-by-value instruction.
        /// </summary>
        public static bool IsLocalLoad(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Ldloc || me.OpCode == OpCodes.Ldloc_S || me.OpCode == OpCodes.Ldloc_0 ||
                   me.OpCode == OpCodes.Ldloc_1 || me.OpCode == OpCodes.Ldloc_2 || me.OpCode == OpCodes.Ldloc_3;
        }

        /// <summary>
        /// Is this instruction a local load-by-reference instruction.
        /// </summary>
        public static bool IsLocalLoadByRef(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Ldloca || me.OpCode == OpCodes.Ldloca_S;
        }

        /// <summary>
        /// Is this instruction a local store instruction.
        /// </summary>
        public static bool IsLocalStore(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Stloc || me.OpCode == OpCodes.Stloc_S || me.OpCode == OpCodes.Stloc_0 ||
                   me.OpCode == OpCodes.Stloc_1 || me.OpCode == OpCodes.Stloc_2 || me.OpCode == OpCodes.Stloc_3;
        }

        /// <summary>
        /// For a local referencing opcode, get the local it is referencing.
        /// </summary>
        public static MsilLocal GetReferencedLocal(this MsilInstruction me)
        {
            if (me.Operand is MsilOperandInline.MsilOperandLocal mol)
                return mol.Value;
            if (me.OpCode == OpCodes.Stloc_0 || me.OpCode == OpCodes.Ldloc_0)
                return new MsilLocal(0);
            if (me.OpCode == OpCodes.Stloc_1 || me.OpCode == OpCodes.Ldloc_1)
                return new MsilLocal(1);
            if (me.OpCode == OpCodes.Stloc_2 || me.OpCode == OpCodes.Ldloc_2)
                return new MsilLocal(2);
            if (me.OpCode == OpCodes.Stloc_3 || me.OpCode == OpCodes.Ldloc_3)
                return new MsilLocal(3);
            throw new ArgumentException($"Can't get referenced local in instruction {me}");
        }
        /// <summary>
        /// Gets an instruction representing a load-by-value from the given local.
        /// </summary>
        /// <param name="local">Local to load</param>
        /// <returns>Loading instruction</returns>
        public static MsilInstruction AsValueLoad(this MsilLocal local)
        {
            switch (local.Index)
            {
                case 0:
                    return new MsilInstruction(OpCodes.Ldloc_0);
                case 1:
                    return new MsilInstruction(OpCodes.Ldloc_1);
                case 2:
                    return new MsilInstruction(OpCodes.Ldloc_2);
                case 3:
                    return new MsilInstruction(OpCodes.Ldloc_3);
                default:
                    return new MsilInstruction(local.Index < 0xFF ? OpCodes.Ldloc_S : OpCodes.Ldloc).InlineValue(local);
            }
        }

        /// <summary>
        /// Gets an instruction representing a store-by-value to the given local.
        /// </summary>
        /// <param name="local">Local to write to</param>
        /// <returns>Loading instruction</returns>
        public static MsilInstruction AsValueStore(this MsilLocal local)
        {
            switch (local.Index)
            {
                case 0:
                    return new MsilInstruction(OpCodes.Stloc_0);
                case 1:
                    return new MsilInstruction(OpCodes.Stloc_1);
                case 2:
                    return new MsilInstruction(OpCodes.Stloc_2);
                case 3:
                    return new MsilInstruction(OpCodes.Stloc_3);
                default:
                    return new MsilInstruction(local.Index < 0xFF ? OpCodes.Stloc_S : OpCodes.Stloc).InlineValue(local);
            }
        }

        /// <summary>
        /// Gets an instruction representing a load-by-reference from the given local.
        /// </summary>
        /// <param name="local">Local to load</param>
        /// <returns>Loading instruction</returns>
        public static MsilInstruction AsReferenceLoad(this MsilLocal local)
        {
            return new MsilInstruction(local.Index < 0xFF ? OpCodes.Ldloca_S : OpCodes.Ldloca).InlineValue(local);
        }
        #endregion

        #region Argument Utils
        /// <summary>
        /// Is this instruction an argument load-by-value instruction.
        /// </summary>
        public static bool IsArgumentLoad(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Ldarg || me.OpCode == OpCodes.Ldarg_S || me.OpCode == OpCodes.Ldarg_0 ||
                   me.OpCode == OpCodes.Ldarg_1 || me.OpCode == OpCodes.Ldarg_2 || me.OpCode == OpCodes.Ldarg_3;
        }

        /// <summary>
        /// Is this instruction an argument load-by-reference instruction.
        /// </summary>
        public static bool IsArgumentLoadByRef(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Ldarga || me.OpCode == OpCodes.Ldarga_S;
        }

        /// <summary>
        /// Is this instruction an argument store instruction.
        /// </summary>
        public static bool IsArgumentStore(this MsilInstruction me)
        {
            return me.OpCode == OpCodes.Starg || me.OpCode == OpCodes.Starg_S;
        }

        /// <summary>
        /// For an argument referencing opcode, get the index of the local it is referencing.
        /// </summary>
        public static MsilArgument GetReferencedArgument(this MsilInstruction me)
        {
            if (me.Operand is MsilOperandInline.MsilOperandArgument mol)
                return mol.Value;
            if (me.OpCode == OpCodes.Ldarg_0)
                return new MsilArgument(0);
            if (me.OpCode == OpCodes.Ldarg_1)
                return new MsilArgument(1);
            if (me.OpCode == OpCodes.Ldarg_2)
                return new MsilArgument(2);
            if (me.OpCode == OpCodes.Ldarg_3)
                return new MsilArgument(3);
            throw new ArgumentException($"Can't get referenced argument in instruction {me}");
        }

        /// <summary>
        /// Gets an instruction representing a load-by-value from the given argument.
        /// </summary>
        /// <param name="argument">argument to load</param>
        /// <returns>Load instruction</returns>
        public static MsilInstruction AsValueLoad(this MsilArgument argument)
        {
            switch (argument.Position)
            {
                case 0:
                    return new MsilInstruction(OpCodes.Ldarg_0);
                case 1:
                    return new MsilInstruction(OpCodes.Ldarg_1);
                case 2:
                    return new MsilInstruction(OpCodes.Ldarg_2);
                case 3:
                    return new MsilInstruction(OpCodes.Ldarg_3);
                default:
                    return new MsilInstruction(argument.Position < 0xFF ? OpCodes.Ldarg_S : OpCodes.Ldarg).InlineValue(argument);
            }
        }

        /// <summary>
        /// Gets an instruction representing a store-by-value to the given argument.
        /// </summary>
        /// <param name="argument">argument to write to</param>
        /// <returns>Store instruction</returns>
        public static MsilInstruction AsValueStore(this MsilArgument argument)
        {
            return new MsilInstruction(argument.Position < 0xFF ? OpCodes.Starg_S : OpCodes.Starg).InlineValue(argument);
        }

        /// <summary>
        /// Gets an instruction representing a load-by-reference from the given argument.
        /// </summary>
        /// <param name="argument">argument to load</param>
        /// <returns>Reference load instruction</returns>
        public static MsilInstruction AsReferenceLoad(this MsilArgument argument)
        {
            return new MsilInstruction(argument.Position < 0xFF ? OpCodes.Ldarga_S : OpCodes.Ldarga).InlineValue(argument);
        }
        #endregion
    }
}
