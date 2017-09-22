using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents an inline value
    /// </summary>
    /// <typeparam name="T">The type of the inline value</typeparam>
    public abstract class MsilOperandInline<T> : MsilOperand
    {
        internal MsilOperandInline(MsilInstruction instruction) : base(instruction)
        {
        }

        /// <summary>
        ///     Inline value
        /// </summary>
        public T Value { get; set; }

        internal override void CopyTo(MsilOperand operand)
        {
            var lt = operand as MsilOperandInline<T>;
            if (lt == null)
                throw new ArgumentException($"Target {operand?.GetType().Name} must be of same type {GetType().Name}", nameof(operand));
            lt.Value = Value;
            ;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value?.ToString() ?? "null";
        }
    }

    /// <summary>
    ///     Registry of different inline operand types
    /// </summary>
    public static class MsilOperandInline
    {
        /// <summary>
        ///     Inline unsigned byte
        /// </summary>
        public class MsilOperandUInt8 : MsilOperandInline<byte>
        {
            internal MsilOperandUInt8(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value = reader.ReadByte();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline signed byte
        /// </summary>
        public class MsilOperandInt8 : MsilOperandInline<sbyte>
        {
            internal MsilOperandInt8(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value =
                    (sbyte)reader.ReadByte();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline integer
        /// </summary>
        public class MsilOperandInt32 : MsilOperandInline<int>
        {
            internal MsilOperandInt32(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value = reader.ReadInt32();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline single
        /// </summary>
        public class MsilOperandSingle : MsilOperandInline<float>
        {
            internal MsilOperandSingle(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value = reader.ReadSingle();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline double
        /// </summary>
        public class MsilOperandDouble : MsilOperandInline<double>
        {
            internal MsilOperandDouble(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value = reader.ReadDouble();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline long
        /// </summary>
        public class MsilOperandInt64 : MsilOperandInline<long>
        {
            internal MsilOperandInt64(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value = reader.ReadInt64();
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline string
        /// </summary>
        public class MsilOperandString : MsilOperandInline<string>
        {
            internal MsilOperandString(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value =
                    context.TokenResolver.ResolveString(reader.ReadInt32());
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline CLR signature
        /// </summary>
        public class MsilOperandSignature : MsilOperandInline<SignatureHelper>
        {
            internal MsilOperandSignature(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                byte[] sig = context.TokenResolver
                    .ResolveSignature(reader.ReadInt32());
                throw new ArgumentException("Can't figure out how to convert this.");
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value);
            }
        }

        /// <summary>
        ///     Inline argument reference
        /// </summary>
        public class MsilOperandArgument : MsilOperandInline<MsilArgument>
        {
            internal MsilOperandArgument(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                int paramID =
                    Instruction.OpCode.OperandType == OperandType.ShortInlineVar
                        ? reader.ReadByte()
                        : reader.ReadUInt16();
                if (paramID == 0 && !context.Method.IsStatic)
                    throw new ArgumentException("Haven't figured out how to ldarg with the \"this\" argument");
                Value = new MsilArgument(context.Method.GetParameters()[paramID - (context.Method.IsStatic ? 0 : 1)]);
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value.Position);
            }
        }

        /// <summary>
        ///     Inline local variable reference
        /// </summary>
        public class MsilOperandLocal : MsilOperandInline<MsilLocal>
        {
            internal MsilOperandLocal(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value =
                    new MsilLocal(context.Method.GetMethodBody().LocalVariables[
                        Instruction.OpCode.OperandType == OperandType.ShortInlineVar
                            ? reader.ReadByte()
                            : reader.ReadUInt16()]);
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value.Index);
            }
        }

        /// <summary>
        ///     Inline <see cref="Type" /> or <see cref="MemberInfo" />
        /// </summary>
        /// <typeparam name="TY">Actual member type</typeparam>
        public class MsilOperandReflected<TY> : MsilOperandInline<TY> where TY : class
        {
            internal MsilOperandReflected(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                object value = null;
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineTok:
                        value = context.TokenResolver.ResolveMember(reader.ReadInt32());
                        break;
                    case OperandType.InlineType:
                        value = context.TokenResolver.ResolveType(reader.ReadInt32());
                        break;
                    case OperandType.InlineMethod:
                        value = context.TokenResolver.ResolveMethod(reader.ReadInt32());
                        break;
                    case OperandType.InlineField:
                        value = context.TokenResolver.ResolveField(reader.ReadInt32());
                        break;
                    default:
                        throw new ArgumentException("Reflected operand only applies to inline reflected types");
                }
                if (value is TY vty)
                    Value = vty;
                else
                    throw new Exception($"Expected type {typeof(TY).Name} from operand {Instruction.OpCode.OperandType}, got {value.GetType()?.Name ?? "null"}");
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                if (Value is ConstructorInfo)
                    generator.Emit(Instruction.OpCode, Value as ConstructorInfo);
                else if (Value is FieldInfo)
                    generator.Emit(Instruction.OpCode, Value as FieldInfo);
                else if (Value is Type)
                    generator.Emit(Instruction.OpCode, Value as Type);
                else if (Value is MethodInfo)
                    generator.Emit(Instruction.OpCode, Value as MethodInfo);
            }
        }
    }
}