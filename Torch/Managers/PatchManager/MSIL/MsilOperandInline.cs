using System;
using System.Diagnostics;
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
        ///     Inline integer
        /// </summary>
        public class MsilOperandInt32 : MsilOperandInline<int>
        {
            internal MsilOperandInt32(MsilInstruction instruction) : base(instruction)
            {
            }

            public override int MaxBytes => Instruction.OpCode.OperandType == OperandType.InlineI ? 4 : 1;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineI:
                        Value = reader.ReadByte();
                        return;
                    case OperandType.InlineI:
                        Value = reader.ReadInt32();
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineI:
                        generator.Emit(Instruction.OpCode, (byte)Value);
                        return;
                    case OperandType.InlineI:
                        generator.Emit(Instruction.OpCode, Value);
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }   
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

            public override int MaxBytes => 4;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineR:
                        Value = reader.ReadSingle();
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineR:
                        generator.Emit(Instruction.OpCode, Value);
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => 8;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineR:
                        Value = reader.ReadDouble();
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineR:
                        generator.Emit(Instruction.OpCode, Value);
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => 8;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineI8:
                        Value = reader.ReadInt64();
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineI8:
                        generator.Emit(Instruction.OpCode, Value);
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => 4;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineString:
                        Value = context.TokenResolver.ResolveString(reader.ReadInt32());
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineString:
                        generator.Emit(Instruction.OpCode, Value);
                        return;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => throw new NotImplementedException();

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineSig:
                        throw new NotImplementedException();
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineSig:
                        throw new NotImplementedException();
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => Instruction.OpCode.OperandType == OperandType.ShortInlineVar ? 1 : 2;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                int id;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineVar:
                        id = reader.ReadByte();
                        break;
                    case OperandType.InlineVar:
                        id = reader.ReadUInt16();
                        break;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }

                if (id == 0 && !context.Method.IsStatic)
                    throw new ArgumentException("Haven't figured out how to ldarg with the \"this\" argument");
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (context.Method == null)
                    Value = new MsilArgument(id);
                else
                    Value = new MsilArgument(context.Method.GetParameters()[id - (context.Method.IsStatic ? 0 : 1)]);
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineVar:
                        generator.Emit(Instruction.OpCode, (byte) Value.Position);
                        break;
                    case OperandType.InlineVar:
                        generator.Emit(Instruction.OpCode, (short)Value.Position);
                        break;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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

            public override int MaxBytes => 2;

            internal override void Read(MethodContext context, BinaryReader reader)
            {
                int id;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineVar:
                        id = reader.ReadByte();
                        break;
                    case OperandType.InlineVar:
                        id = reader.ReadUInt16();
                        break;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (context.MethodBody == null)
                    Value = new MsilLocal(id);
                else
                    Value = new MsilLocal(context.MethodBody.LocalVariables[id]);
            }

            internal override void Emit(LoggingIlGenerator generator)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.ShortInlineVar:
                        generator.Emit(Instruction.OpCode, (byte)Value.Index);
                        break;
                    case OperandType.InlineVar:
                        generator.Emit(Instruction.OpCode, (short)Value.Index);
                        break;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
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
            
            public override int MaxBytes => 4;

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
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }
                if (value is TY vty)
                    Value = vty;
                else if (value == null)
                    Value = null;
                else
                    throw new Exception($"Expected type {typeof(TY).Name} from operand {Instruction.OpCode.OperandType}, got {value.GetType()?.Name ?? "null"}");
            }

            internal override void Emit(LoggingIlGenerator generator)
            {

                switch (Instruction.OpCode.OperandType)
                {
                    case OperandType.InlineTok:
                        Debug.Assert(Value is MethodBase || Value is Type || Value is FieldInfo,
                            $"Value {Value?.GetType()} doesn't match operand type for {Instruction.OpCode}");
                        break;
                    case OperandType.InlineType:
                        Debug.Assert(Value is Type, $"Value {Value?.GetType()} doesn't match operand type for {Instruction.OpCode}");
                        break;
                    case OperandType.InlineMethod:
                        Debug.Assert(Value is MethodBase, $"Value {Value?.GetType()} doesn't match operand type for {Instruction.OpCode}");
                        break;
                    case OperandType.InlineField:
                        Debug.Assert(Value is FieldInfo, $"Value {Value?.GetType()} doesn't match operand type for {Instruction.OpCode}");
                        break;
                    default:
                        throw new InvalidBranchException(
                            $"OpCode {Instruction.OpCode}, operand type {Instruction.OpCode.OperandType} doesn't match {GetType().Name}");
                }

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