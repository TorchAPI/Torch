using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;

namespace Torch.Managers.PatchManager.Transpile
{
    internal class MethodContext
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public readonly MethodBase Method;
        public readonly MethodBody MethodBody;
        private readonly byte[] _msilBytes;

        internal Dictionary<int, MsilLabel> Labels { get; } = new Dictionary<int, MsilLabel>();
        private readonly List<MsilInstruction> _instructions = new List<MsilInstruction>();
        public IReadOnlyList<MsilInstruction> Instructions => _instructions;

        internal ITokenResolver TokenResolver { get; }

        internal MsilLabel LabelAt(int i)
        {
            if (Labels.TryGetValue(i, out MsilLabel label))
                return label;
            Labels.Add(i, label = new MsilLabel());
            return label;
        }

        public MethodContext(MethodBase method)
        {
            Method = method;
            MethodBody = method.GetMethodBody();
            Debug.Assert(MethodBody != null, "Method body is null");
            _msilBytes = MethodBody.GetILAsByteArray();
            TokenResolver = new NormalTokenResolver(method);
        }


#pragma warning disable 649
        [ReflectedMethod(Name = "BakeByteArray")] private static Func<ILGenerator, byte[]> _ilGeneratorBakeByteArray;
#pragma warning restore 649

        public MethodContext(DynamicMethod method)
        {
            Method = null;
            MethodBody = null;
            _msilBytes = _ilGeneratorBakeByteArray(method.GetILGenerator());
            TokenResolver = new DynamicMethodTokenResolver(method);
        }

        public void Read()
        {
            ReadInstructions();
            ResolveLabels();
            ResolveCatchClauses();
        }

        private void ReadInstructions()
        {
            Labels.Clear();
            _instructions.Clear();
            using (var memory = new MemoryStream(_msilBytes))
            using (var reader = new BinaryReader(memory))
                while (memory.Length > memory.Position)
                {
                    var opcodeOffset = (int) memory.Position;
                    var instructionValue = (short) memory.ReadByte();
                    if (Prefixes.Contains(instructionValue))
                    {
                        instructionValue = (short) ((instructionValue << 8) | memory.ReadByte());
                    }
                    if (!OpCodeLookup.TryGetValue(instructionValue, out OpCode opcode))
                    {
                        var msg = $"Unknown opcode {instructionValue:X}";
                        _log.Error(msg);
                        Debug.Assert(false, msg);
                        continue;
                    }
                    if (opcode.Size != memory.Position - opcodeOffset)
                        throw new Exception(
                            $"Opcode said it was {opcode.Size} but we read {memory.Position - opcodeOffset}");
                    var instruction = new MsilInstruction(opcode)
                    {
                        Offset = opcodeOffset
                    };
                    _instructions.Add(instruction);
                    instruction.Operand?.Read(this, reader);
                }
        }

        private void ResolveCatchClauses()
        {
            if (MethodBody == null)
                return;
            foreach (ExceptionHandlingClause clause in MethodBody.ExceptionHandlingClauses)
            {
                var beginInstruction = FindInstruction(clause.TryOffset);
                var catchInstruction = FindInstruction(clause.HandlerOffset);
                var finalInstruction = FindInstruction(clause.HandlerOffset + clause.HandlerLength);
                beginInstruction.TryCatchOperation =
                    new MsilTryCatchOperation(MsilTryCatchOperationType.BeginExceptionBlock);
                if ((clause.Flags & ExceptionHandlingClauseOptions.Fault) != 0)
                    catchInstruction.TryCatchOperation =
                        new MsilTryCatchOperation(MsilTryCatchOperationType.BeginFaultBlock);
                else if ((clause.Flags & ExceptionHandlingClauseOptions.Finally) != 0)
                    catchInstruction.TryCatchOperation =
                        new MsilTryCatchOperation(MsilTryCatchOperationType.BeginFinallyBlock);
                else
                    catchInstruction.TryCatchOperation =
                        new MsilTryCatchOperation(MsilTryCatchOperationType.BeginClauseBlock, clause.CatchType);

                finalInstruction.TryCatchOperation =
                    new MsilTryCatchOperation(MsilTryCatchOperationType.EndExceptionBlock);
            }
        }

        public MsilInstruction FindInstruction(int offset)
        {
            int min = 0, max = _instructions.Count;
            while (min != max)
            {
                int mid = (min + max) / 2;
                if (_instructions[mid].Offset < offset)
                    min = mid + 1;
                else
                    max = mid;
            }
            return min >= 0 && min < _instructions.Count ? _instructions[min] : null;
        }

        private void ResolveLabels()
        {
            foreach (var label in Labels)
            {
                MsilInstruction target = FindInstruction(label.Key);
                Debug.Assert(target != null, $"No label for offset {label.Key}");
                target?.Labels?.Add(label.Value);
            }
        }


        public string ToHumanMsil()
        {
            return string.Join("\n", _instructions.Select(x => $"IL_{x.Offset:X4}: {x.StackChange():+0;-#} {x}"));
        }

        private static readonly Dictionary<short, OpCode> OpCodeLookup;
        private static readonly HashSet<short> Prefixes;

        static MethodContext()
        {
            OpCodeLookup = new Dictionary<short, OpCode>();
            Prefixes = new HashSet<short>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var opcode = (OpCode) field.GetValue(null);
                if (opcode.OpCodeType != OpCodeType.Nternal)
                    OpCodeLookup.Add(opcode.Value, opcode);
                if ((ushort) opcode.Value > 0xFF)
                {
                    Prefixes.Add((short) ((ushort) opcode.Value >> 8));
                }
            }
        }
    }
}