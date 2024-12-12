using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Managers.PatchManager
{
    internal static class EmitExtensions
    {
        /// <summary>
        /// Sets the given local to its default value in the given IL generator.
        /// </summary>
        /// <param name="local">Local to set to default</param>
        /// <returns>Instructions</returns>
        public static IEnumerable<MsilInstruction> SetToDefault(this MsilLocal local)
        {
            Debug.Assert(local.Type != null);
            if (local.Type.IsEnum || local.Type.IsPrimitive)
            {
                if (local.Type == typeof(float))
                    yield return new MsilInstruction(OpCodes.Ldc_R4).InlineValue(0f);
                else if (local.Type == typeof(double))
                    yield return new MsilInstruction(OpCodes.Ldc_R8).InlineValue(0d);
                else if (local.Type == typeof(long) || local.Type == typeof(ulong))
                    yield return new MsilInstruction(OpCodes.Ldc_I8).InlineValue(0L);
                else
                    yield return new MsilInstruction(OpCodes.Ldc_I4).InlineValue(0);
                yield return new MsilInstruction(OpCodes.Stloc).InlineValue(local);
            }
            else if (local.Type.IsValueType) // struct
            {
                yield return new MsilInstruction(OpCodes.Ldloca).InlineValue(local);
                yield return new MsilInstruction(OpCodes.Initobj).InlineValue(local.Type);
            }
            else // class
            {
                yield return new MsilInstruction(OpCodes.Ldnull);
                yield return new MsilInstruction(OpCodes.Stloc).InlineValue(local);
            }
        }

        /// <summary>
        /// Emits a dereference for the given type.
        /// </summary>
        /// <param name="type">Type to dereference</param>
        /// <returns>Derference instruction</returns>
        public static MsilInstruction EmitDereference(Type type)
        {
            if (type.IsByRef)
                type = type.GetElementType();
            Debug.Assert(type != null);

            if (type == typeof(float))
                return new MsilInstruction(OpCodes.Ldind_R4);
            if (type == typeof(double))
                return new MsilInstruction(OpCodes.Ldind_R8);
            if (type == typeof(byte))
                return new MsilInstruction(OpCodes.Ldind_U1);
            if (type == typeof(ushort) || type == typeof(char))
                return new MsilInstruction(OpCodes.Ldind_U2);
            if (type == typeof(uint))
                return new MsilInstruction(OpCodes.Ldind_U4);
            if (type == typeof(sbyte))
                return new MsilInstruction(OpCodes.Ldind_I1);
            if (type == typeof(short))
                return new MsilInstruction(OpCodes.Ldind_I2);
            if (type == typeof(int) || type.IsEnum)
                return new MsilInstruction(OpCodes.Ldind_I4);
            if (type == typeof(long) || type == typeof(ulong))
                return new MsilInstruction(OpCodes.Ldind_I8);
            return new MsilInstruction(OpCodes.Ldind_Ref);
        }
    }
}
