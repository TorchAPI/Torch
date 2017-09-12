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
            _msilBytes = Method.GetMethodBody().GetILAsByteArray();
            TokenResolver = new NormalTokenResolver(method);
        }

        public void Read()
        {
            ReadInstructions();
            ResolveLabels();
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
                    var instructionValue = (short)memory.ReadByte();
                    if (Prefixes.Contains(instructionValue))
                    {
                        instructionValue = (short)((instructionValue << 8) | memory.ReadByte());
                    }
                    if (!OpCodeLookup.TryGetValue(instructionValue, out OpCode opcode))
                        throw new Exception($"Unknown opcode {instructionValue:X}");
                    if (opcode.Size != memory.Position - opcodeOffset)
                        throw new Exception($"Opcode said it was {opcode.Size} but we read {memory.Position - opcodeOffset}");
                    var instruction = new MsilInstruction(opcode)
                    {
                        Offset = opcodeOffset
                    };
                    _instructions.Add(instruction);
                    instruction.Operand?.Read(this, reader);
                }
        }

        private void ResolveLabels()
        {
            foreach (var label in Labels)
            {
                int min = 0, max = _instructions.Count;
                while (min != max)
                {
                    int mid = (min + max) / 2;
                    if (_instructions[mid].Offset < label.Key)
                        min = mid + 1;
                    else
                        max = mid;
                }
#if DEBUG
                if (min >= _instructions.Count || min < 0)
                {
                    _log.Trace(
                        $"Want offset {label.Key} for {label.Value}, instruction offsets at\n {string.Join("\n", _instructions.Select(x => $"IL_{x.Offset:X4} {x}"))}");
                }
                MsilInstruction prevInsn = min > 0 ? _instructions[min - 1] : null;
                if ((prevInsn == null || prevInsn.Offset >= label.Key) ||
                    _instructions[min].Offset < label.Key)
                    _log.Error($"Label {label.Value} wanted {label.Key} but instruction is at {_instructions[min].Offset}.  Previous instruction is at {prevInsn?.Offset ?? -1}");
#endif
                _instructions[min]?.Labels?.Add(label.Value);
            }
        }


        [Conditional("DEBUG")]
        public void CheckIntegrity()
        {
            var entryStackCount = new Dictionary<MsilLabel, Dictionary<MsilInstruction, int>>();
            var currentStackSize = 0;
            foreach (MsilInstruction insn in _instructions)
            {
                // I don't want to deal with this, so I won't
                if (insn.OpCode == OpCodes.Br || insn.OpCode == OpCodes.Br_S || insn.OpCode == OpCodes.Jmp ||
                    insn.OpCode == OpCodes.Leave || insn.OpCode == OpCodes.Leave_S)
                    break;
                foreach (MsilLabel label in insn.Labels)
                    if (entryStackCount.TryGetValue(label, out Dictionary<MsilInstruction, int> dict))
                        dict.Add(insn, currentStackSize);
                    else
                        (entryStackCount[label] = new Dictionary<MsilInstruction, int>()).Add(insn, currentStackSize);

                currentStackSize += insn.StackChange();

                if (insn.Operand is MsilOperandBrTarget br)
                    if (entryStackCount.TryGetValue(br.Target, out Dictionary<MsilInstruction, int> dict))
                        dict.Add(insn, currentStackSize);
                    else
                        (entryStackCount[br.Target] = new Dictionary<MsilInstruction, int>()).Add(insn, currentStackSize);
            }
            foreach (KeyValuePair<MsilLabel, Dictionary<MsilInstruction, int>> label in entryStackCount)
            {
                if (label.Value.Values.Aggregate(new HashSet<int>(), (a, b) =>
                {
                    a.Add(b);
                    return a;
                }).Count > 1)
                {
                    _log.Warn($"Label {label.Key} has multiple entry stack counts");
                    foreach (KeyValuePair<MsilInstruction, int> kv in label.Value)
                        _log.Warn($"{kv.Key.Offset:X4} {kv.Key} => {kv.Value}");
                }
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
                var opcode = (OpCode)field.GetValue(null);
                if (opcode.OpCodeType != OpCodeType.Nternal)
                    OpCodeLookup.Add(opcode.Value, opcode);
                if ((ushort)opcode.Value > 0xFF)
                {
                    Prefixes.Add((short)((ushort)opcode.Value >> 8));
                }
            }
        }
    }
}
