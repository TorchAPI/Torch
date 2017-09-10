using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Torch.Managers.PatchManager.MSIL;

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
                    var count = 1;
                    var instructionValue = (short)memory.ReadByte();
                    if (Prefixes.Contains(instructionValue))
                    {
                        instructionValue = (short) ((instructionValue << 8) | memory.ReadByte());
                        count++;
                    }
                    if (!OpCodeLookup.TryGetValue(instructionValue, out OpCode opcode))
                        throw new Exception($"Unknown opcode {instructionValue:X}");
                    if (opcode.Size != count)
                        throw new Exception($"Opcode said it was {opcode.Size} but we read {count}");
                    var instruction = new MsilInstruction(opcode)
                    {
                        Offset = (int) memory.Position
                    };
                    _instructions.Add(instruction);
                    instruction.Operand?.Read(this, reader);
                }
        }

        private void ResolveLabels()
        {
            foreach (var label in Labels)
            {
                int min = 0, max = _instructions.Count - 1;
                while (min <= max)
                {
                    var mid = min + ((max - min) / 2);
                    if (label.Key < _instructions[mid].Offset)
                        max = mid - 1;
                    else
                        min = mid + 1;
                }
                _instructions[min]?.Labels?.Add(label.Value);
            }
        }

        public string ToHumanMsil()
        {
            return string.Join("\n", _instructions.Select(x => $"IL_{x.Offset:X4}: {x}"));
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
                if ((ushort) opcode.Value > 0xFF)
                {
                    Prefixes.Add((short) ((ushort) opcode.Value >> 8));
                }
            }
        }
    }
}
