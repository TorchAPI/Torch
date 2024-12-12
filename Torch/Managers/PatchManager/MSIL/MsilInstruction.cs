using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Torch.Utils;

namespace Torch.Managers.PatchManager.MSIL
{
    /// <summary>
    ///     Represents a single MSIL instruction, and its operand
    /// </summary>
    public class MsilInstruction
    {
        /// <summary>
        ///     Creates a new instruction with the given opcode.
        /// </summary>
        /// <param name="opcode">Opcode</param>
        public MsilInstruction(OpCode opcode)
        {
            OpCode = opcode;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    Operand = null;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    Operand = new MsilOperandBrTarget(this);
                    break;
                case OperandType.InlineField:
                    Operand = new MsilOperandInline.MsilOperandReflected<FieldInfo>(this);
                    break;
                case OperandType.ShortInlineI:
                case OperandType.InlineI:
                    Operand = new MsilOperandInline.MsilOperandInt32(this);
                    break;
                case OperandType.InlineI8:
                    Operand = new MsilOperandInline.MsilOperandInt64(this);
                    break;
                case OperandType.InlineMethod:
                    Operand = new MsilOperandInline.MsilOperandReflected<MethodBase>(this);
                    break;
                case OperandType.InlineR:
                    Operand = new MsilOperandInline.MsilOperandDouble(this);
                    break;
                case OperandType.InlineSig:
                    Operand = new MsilOperandInline.MsilOperandSignature(this);
                    break;
                case OperandType.InlineString:
                    Operand = new MsilOperandInline.MsilOperandString(this);
                    break;
                case OperandType.InlineSwitch:
                    Operand = new MsilOperandSwitch(this);
                    break;
                case OperandType.InlineTok:
                    Operand = new MsilOperandInline.MsilOperandReflected<MemberInfo>(this);
                    break;
                case OperandType.InlineType:
                    Operand = new MsilOperandInline.MsilOperandReflected<Type>(this);
                    break;
                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                    if (OpCode.IsLocalStore() || OpCode.IsLocalLoad() || OpCode.IsLocalLoadByRef())
                        Operand = new MsilOperandInline.MsilOperandLocal(this);
                    else
                        Operand = new MsilOperandInline.MsilOperandArgument(this);
                    break;
                case OperandType.ShortInlineR:
                    Operand = new MsilOperandInline.MsilOperandSingle(this);
                    break;
#pragma warning disable 618
                case OperandType.InlinePhi:
#pragma warning restore 618
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Opcode of this instruction
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        ///     Raw memory offset of this instruction; optional.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        ///     The operand for this instruction, or null.
        /// </summary>
        public MsilOperand Operand { get; }

        /// <summary>
        ///     Labels pointing to this instruction.
        /// </summary>
        public HashSet<MsilLabel> Labels { get; } = new HashSet<MsilLabel>();

        /// <summary>
        /// The try catch operation that is performed here.
        /// </summary>
        [Obsolete("Since instructions can have multiple try catch operations you need to be using TryCatchOperations")]
        public MsilTryCatchOperation TryCatchOperation
        {
            get => TryCatchOperations.FirstOrDefault();
            set
            {
                TryCatchOperations.Clear();
                if (value != null)
                    TryCatchOperations.Add(value);
            }
        }

        /// <summary>
        /// The try catch operations performed here, in order from first to last.
        /// </summary>
        public readonly List<MsilTryCatchOperation> TryCatchOperations = new List<MsilTryCatchOperation>();

        private static readonly ConcurrentDictionary<Type, PropertyInfo> _setterInfoForInlines = new ConcurrentDictionary<Type, PropertyInfo>();

        /// <summary>
        ///     Sets the inline value for this instruction.
        /// </summary>
        /// <typeparam name="T">The type of the inline constraint</typeparam>
        /// <param name="o">Value</param>
        /// <returns>This instruction</returns>
        public MsilInstruction InlineValue<T>(T o)
        {
            Type type = typeof(T);
            while (type != null)
            {
                if (!_setterInfoForInlines.TryGetValue(type, out PropertyInfo target))
                {
                    Type genType = typeof(MsilOperandInline<>).MakeGenericType(type);
                    target = genType.GetProperty(nameof(MsilOperandInline<int>.Value));
                    _setterInfoForInlines[type] = target;
                }

                Debug.Assert(target?.DeclaringType != null);
                if (target.DeclaringType.IsInstanceOfType(Operand))
                {
                    target.SetValue(Operand, o);
                    return this;
                }

                type = type.BaseType;
            }

            ((MsilOperandInline<T>) Operand).Value = o;
            return this;
        }

        /// <summary>
        /// Makes a copy of the instruction with a new opcode.
        /// </summary>
        /// <param name="newOpcode">The new opcode</param>
        /// <returns>The copy</returns>
        public MsilInstruction CopyWith(OpCode newOpcode)
        {
            var result = new MsilInstruction(newOpcode);
            Operand?.CopyTo(result.Operand);
            foreach (MsilLabel x in Labels)
                result.Labels.Add(x);
            foreach (var op in TryCatchOperations)
                result.TryCatchOperations.Add(op);
            return result;
        }

        /// <summary>
        /// Adds the given label to this instruction
        /// </summary>
        /// <param name="label">Label to add</param>
        /// <returns>this instruction</returns>
        public MsilInstruction LabelWith(MsilLabel label)
        {
            Labels.Add(label);
            return this;
        }

        /// <summary>
        ///     Sets the inline branch target for this instruction.
        /// </summary>
        /// <param name="label">Target to jump to</param>
        /// <returns>This instruction</returns>
        public MsilInstruction InlineTarget(MsilLabel label)
        {
            ((MsilOperandBrTarget) Operand).Target = label;
            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (MsilLabel label in Labels)
                sb.Append(label).Append(": ");
            sb.Append(OpCode.Name).Append("\t").Append(Operand);
            return sb.ToString();
        }


#pragma warning disable 649
        [ReflectedMethod(Name = "StackChange")]
        private static Func<OpCode, int> _stackChange;
#pragma warning restore 649

        /// <summary>
        /// Estimates the stack delta for this instruction.
        /// </summary>
        /// <returns>Stack delta</returns>
        public int StackChange()
        {
            int num = _stackChange.Invoke(OpCode);
            if ((OpCode == OpCodes.Call || OpCode == OpCodes.Callvirt || OpCode == OpCodes.Newobj) &&
                Operand is MsilOperandInline<MethodBase> inline)
            {
                MethodBase op = inline.Value;
                if (op == null)
                    return num;
                if (op is MethodInfo mi && mi.ReturnType != typeof(void))
                    num++;
                num -= op.GetParameters().Length;
                if (!op.IsStatic && OpCode != OpCodes.Newobj)
                    num--;
            }

            return num;
        }

        /// <summary>
        /// Gets the maximum amount of space this instruction will use.
        /// </summary>
        public int MaxBytes => 2 + (Operand?.MaxBytes ?? 0);
    }
}
