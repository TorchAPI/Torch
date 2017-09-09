using System;
using System.Diagnostics;
using System.Reflection.Emit;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager
{
    internal static class EmitExtensions
    {
        /// <summary>
        /// Sets the given local to its default value in the given IL generator.
        /// </summary>
        /// <param name="local">Local to set to default</param>
        /// <param name="target">The IL generator</param>
        public static void SetToDefault(this LocalBuilder local, LoggingIlGenerator target)
        {
            Debug.Assert(local.LocalType != null);
            if (local.LocalType.IsEnum || local.LocalType.IsPrimitive)
            {
                if (local.LocalType == typeof(float))
                    target.Emit(OpCodes.Ldc_R4, 0f);
                else if (local.LocalType == typeof(double))
                    target.Emit(OpCodes.Ldc_R8, 0d);
                else if (local.LocalType == typeof(long) || local.LocalType == typeof(ulong))
                    target.Emit(OpCodes.Ldc_I8, 0L);
                else
                    target.Emit(OpCodes.Ldc_I4, 0);
                target.Emit(OpCodes.Stloc, local);
            }
            else if (local.LocalType.IsValueType) // struct
            {
                target.Emit(OpCodes.Ldloca, local);
                target.Emit(OpCodes.Initobj, local.LocalType);
            }
            else // class
            {
                target.Emit(OpCodes.Ldnull);
                target.Emit(OpCodes.Stloc, local);
            }
        }

        /// <summary>
        /// Emits a dereference for the given type.
        /// </summary>
        /// <param name="target">IL Generator to emit on</param>
        /// <param name="type">Type to dereference</param>
        public static void EmitDereference(this LoggingIlGenerator target, Type type)
        {
            if (type.IsByRef)
                type = type.GetElementType();
            Debug.Assert(type != null);

            if (type == typeof(float))
                target.Emit(OpCodes.Ldind_R4);
            else if (type == typeof(double))
                target.Emit(OpCodes.Ldind_R8);
            else if (type == typeof(byte))
                target.Emit(OpCodes.Ldind_U1);
            else if (type == typeof(ushort) || type == typeof(char))
                target.Emit(OpCodes.Ldind_U2);
            else if (type == typeof(uint))
                target.Emit(OpCodes.Ldind_U4);
            else if (type == typeof(sbyte))
                target.Emit(OpCodes.Ldind_I1);
            else if (type == typeof(short))
                target.Emit(OpCodes.Ldind_I2);
            else if (type == typeof(int) || type.IsEnum)
                target.Emit(OpCodes.Ldind_I4);
            else if (type == typeof(long) || type == typeof(ulong))
                target.Emit(OpCodes.Ldind_I8);
            else
                target.Emit(OpCodes.Ldind_Ref);
        }
    }
}
