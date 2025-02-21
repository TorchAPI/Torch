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
        [ReflectedMethod(Name = "BakeByteArray")]
        private static Func<ILGenerator, byte[]> _ilGeneratorBakeByteArray;

        [ReflectedMethod(Name = "GetExceptions")]
        private static Func<ILGenerator, Array> _ilGeneratorGetExceptionHandlers;

        private const string InternalExceptionInfo = "System.Reflection.Emit.__ExceptionInfo, mscorlib";

        [ReflectedMethod(Name = "GetExceptionTypes", TypeName = InternalExceptionInfo)]
        private static Func<object, int[]> _exceptionHandlerGetTypes;

        [ReflectedMethod(Name = "GetStartAddress", TypeName = InternalExceptionInfo)]
        private static Func<object, int> _exceptionHandlerGetStart;

        [ReflectedMethod(Name = "GetEndAddress", TypeName = InternalExceptionInfo)]
        private static Func<object, int> _exceptionHandlerGetEnd;

        [ReflectedMethod(Name = "GetFinallyEndAddress", TypeName = InternalExceptionInfo)]
        private static Func<object, int> _exceptionHandlerGetFinallyEnd;

        [ReflectedMethod(Name = "GetNumberOfCatches", TypeName = InternalExceptionInfo)]
        private static Func<object, int> _exceptionHandlerGetCatchCount;

        [ReflectedMethod(Name = "GetCatchAddresses", TypeName = InternalExceptionInfo)]
        private static Func<object, int[]> _exceptionHandlerGetCatchAddrs;

        [ReflectedMethod(Name = "GetCatchEndAddresses", TypeName = InternalExceptionInfo)]
        private static Func<object, int[]> _exceptionHandlerGetCatchEndAddrs;

        [ReflectedMethod(Name = "GetFilterAddresses", TypeName = InternalExceptionInfo)]
        private static Func<object, int[]> _exceptionHandlerGetFilterAddrs;
#pragma warning restore 649

        private readonly Array _dynamicExceptionTable;

        public MethodContext(DynamicMethod method)
        {
            Method = null;
            MethodBody = null;
            _msilBytes = _ilGeneratorBakeByteArray(method.GetILGenerator());
            _dynamicExceptionTable = _ilGeneratorGetExceptionHandlers(method.GetILGenerator());
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
            if (MethodBody != null)
                foreach (var clause in MethodBody.ExceptionHandlingClauses)
                {
                    AddEhHandler(clause.TryOffset, MsilTryCatchOperationType.BeginExceptionBlock);
                    if ((clause.Flags & ExceptionHandlingClauseOptions.Fault) != 0)
                        AddEhHandler(clause.HandlerOffset, MsilTryCatchOperationType.BeginFaultBlock);
                    else if ((clause.Flags & ExceptionHandlingClauseOptions.Finally) != 0)
                        AddEhHandler(clause.HandlerOffset, MsilTryCatchOperationType.BeginFinallyBlock);
                    else
                        AddEhHandler(clause.HandlerOffset, MsilTryCatchOperationType.BeginClauseBlock, clause.CatchType);
                    AddEhHandler(clause.HandlerOffset + clause.HandlerLength, MsilTryCatchOperationType.EndExceptionBlock);
                }

            if (_dynamicExceptionTable != null)
                foreach (var eh in _dynamicExceptionTable)
                {
                    var catchCount = _exceptionHandlerGetCatchCount(eh);
                    var exTypes = _exceptionHandlerGetTypes(eh);
                    var exCatches = _exceptionHandlerGetCatchAddrs(eh);
                    var exCatchesEnd = _exceptionHandlerGetCatchEndAddrs(eh);
                    var exFilters = _exceptionHandlerGetFilterAddrs(eh);
                    var tryAddr = _exceptionHandlerGetStart(eh);
                    var endAddr = _exceptionHandlerGetEnd(eh);
                    var endFinallyAddr = _exceptionHandlerGetFinallyEnd(eh);
                    for (var i = 0; i < catchCount; i++)
                    {
                        var flags = (ExceptionHandlingClauseOptions) exTypes[i];
                        var endAddress = (flags & ExceptionHandlingClauseOptions.Finally) != 0 ? endFinallyAddr : endAddr;

                        var catchAddr = exCatches[i];
                        var catchEndAddr = exCatchesEnd[i];
                        var filterAddr = exFilters[i];
                        
                        AddEhHandler(tryAddr, MsilTryCatchOperationType.BeginExceptionBlock);
                        if ((flags & ExceptionHandlingClauseOptions.Fault) != 0)
                            AddEhHandler(catchAddr, MsilTryCatchOperationType.BeginFaultBlock);
                        else if ((flags & ExceptionHandlingClauseOptions.Finally) != 0)
                            AddEhHandler(catchAddr, MsilTryCatchOperationType.BeginFinallyBlock);
                        else
                            AddEhHandler(catchAddr, MsilTryCatchOperationType.BeginClauseBlock);
                        AddEhHandler(catchEndAddr, MsilTryCatchOperationType.EndExceptionBlock);
                    }
                }
        }

        private void AddEhHandler(int offset, MsilTryCatchOperationType op, Type type = null)
        {
            var instruction = FindInstruction(offset);
            instruction.TryCatchOperations.Add(new MsilTryCatchOperation(op, type) {NativeOffset = offset});
            instruction.TryCatchOperations.Sort((a, b) => a.NativeOffset.CompareTo(b.NativeOffset));
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