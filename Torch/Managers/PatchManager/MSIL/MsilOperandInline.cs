using System;
using System.IO;
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
                    (sbyte) reader.ReadByte();
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
        ///     Inline parameter reference
        /// </summary>
        public class MsilOperandParameter : MsilOperandInline<ParameterInfo>
        {
            internal MsilOperandParameter(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value =
                    context.Method.GetParameters()[
                        Instruction.OpCode.OperandType == OperandType.ShortInlineVar
                            ? reader.ReadByte()
                            : reader.ReadUInt16()];
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value.Position);
            }
        }

        /// <summary>
        ///     Inline local variable reference
        /// </summary>
        public class MsilOperandLocal : MsilOperandInline<LocalVariableInfo>
        {
            internal MsilOperandLocal(MsilInstruction instruction) : base(instruction)
            {
            }

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                Value =
                    context.Method.GetMethodBody().LocalVariables[
                        Instruction.OpCode.OperandType == OperandType.ShortInlineVar
                            ? reader.ReadByte()
                            : reader.ReadUInt16()];
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                generator.Emit(Instruction.OpCode, Value.LocalIndex);
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
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineTok:
                        Value = context.TokenResolver.ResolveMember(reader.ReadInt32()) as TY;
                        break;
                    case OperandType.InlineType:
                        Value = context.TokenResolver.ResolveType(reader.ReadInt32()) as TY;
                        break;
                    case OperandType.InlineMethod:
                        Value = context.TokenResolver.ResolveMethod(reader.ReadInt32()) as TY;
                        break;
                    case OperandType.InlineField:
                        Value = context.TokenResolver.ResolveField(reader.ReadInt32()) as TY;
                        break;
                    default:
                        throw new ArgumentException("Reflected operand only applies to inline reflected types");
                }
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