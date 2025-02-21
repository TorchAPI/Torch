using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NLog;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Managers.PatchManager.Transpile
{
    internal class MethodTranspiler
    {
        public static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal static IEnumerable<MsilInstruction> Transpile(MethodBase baseMethod, Func<Type, MsilLocal> localCreator, IEnumerable<MethodInfo> transpilers,
            MsilLabel retLabel)
        {
            var context = new MethodContext(baseMethod);
            context.Read();
            // IntegrityAnalysis(LogLevel.Trace, context.Instructions);
            return Transpile(baseMethod, context.Instructions, localCreator, transpilers, retLabel);
        }

        internal static IEnumerable<MsilInstruction> Transpile(MethodBase baseMethod, IEnumerable<MsilInstruction> methodContent,
            Func<Type, MsilLocal> localCreator, IEnumerable<MethodInfo> transpilers, MsilLabel retLabel)
        {
            foreach (MethodInfo transpiler in transpilers)
            {
                var paramList = new List<object>();
                foreach (var parameter in transpiler.GetParameters())
                {
                    if (parameter.Name.Equals("__methodBody"))
                        paramList.Add(baseMethod.GetMethodBody());
                    else if (parameter.Name.Equals("__methodBase"))
                        paramList.Add(baseMethod);
                    else if (parameter.Name.Equals("__localCreator"))
                        paramList.Add(localCreator);
                    else if (parameter.ParameterType == typeof(IEnumerable<MsilInstruction>))
                        paramList.Add(methodContent);
                    else
                        throw new ArgumentException(
                            $"Bad transpiler parameter type {parameter.ParameterType.FullName} {parameter.Name}");
                }

                methodContent = (IEnumerable<MsilInstruction>) transpiler.Invoke(null, paramList.ToArray());
            }

            return FixBranchAndReturn(methodContent, retLabel);
        }

        internal static void EmitMethod(IReadOnlyList<MsilInstruction> source, LoggingIlGenerator target)
        {
            var instructions = source.ToArray();
            var offsets = new int[instructions.Length];
            // Calc worst case offsets
            {
                var j = 0;
                for (var i = 0; i < instructions.Length; i++)
                {
                    offsets[i] = j;
                    j += instructions[i].MaxBytes;
                }
            }

            // Perform label markup
            var targets = new Dictionary<MsilLabel, int>();
            for (var i = 0; i < instructions.Length; i++)
                foreach (var label in instructions[i].Labels)
                {
                    if (targets.TryGetValue(label, out var other))
                        _log.Warn($"Label {label} is applied to ({i}: {instructions[i]}) and ({other}: {instructions[other]})");
                    targets[label] = i;
                }

            // Simplify branch instructions
            for (var i = 0; i < instructions.Length; i++)
            {
                var existing = instructions[i];
                if (existing.Operand is MsilOperandBrTarget brOperand && _longToShortBranch.TryGetValue(existing.OpCode, out var shortOpcode))
                {
                    var targetIndex = targets[brOperand.Target];
                    var delta = offsets[targetIndex] - offsets[i];
                    if (sbyte.MinValue < delta && delta < sbyte.MaxValue)
                        instructions[i] = instructions[i].CopyWith(shortOpcode);
                }
            }

            for (var i = 0; i < instructions.Length; i++)
            {
                MsilInstruction il = instructions[i];
                foreach (var tro in il.TryCatchOperations)
                    switch (tro.Type)
                    {
                        case MsilTryCatchOperationType.BeginExceptionBlock:
                            target.BeginExceptionBlock();
                            break;
                        case MsilTryCatchOperationType.BeginClauseBlock:
                            target.BeginCatchBlock(tro.CatchType);
                            break;
                        case MsilTryCatchOperationType.BeginFaultBlock:
                            target.BeginFaultBlock();
                            break;
                        case MsilTryCatchOperationType.BeginFinallyBlock:
                            target.BeginFinallyBlock();
                            break;
                        case MsilTryCatchOperationType.EndExceptionBlock:
                            target.EndExceptionBlock();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                foreach (MsilLabel label in il.Labels)
                    target.MarkLabel(label.LabelFor(target));

                MsilInstruction ilNext = i < instructions.Length - 1 ? instructions[i + 1] : null;

                // Leave opcodes emitted by these:
                if (il.OpCode == OpCodes.Endfilter && ilNext != null &&
                    ilNext.TryCatchOperations.Any(x => x.Type == MsilTryCatchOperationType.BeginClauseBlock))
                    continue;
                if ((il.OpCode == OpCodes.Leave || il.OpCode == OpCodes.Leave_S) && ilNext != null &&
                    ilNext.TryCatchOperations.Any(x => x.Type == MsilTryCatchOperationType.EndExceptionBlock ||
                                                       x.Type == MsilTryCatchOperationType.BeginClauseBlock ||
                                                       x.Type == MsilTryCatchOperationType.BeginFaultBlock ||
                                                       x.Type == MsilTryCatchOperationType.BeginFinallyBlock))
                    continue;
                if ((il.OpCode == OpCodes.Leave || il.OpCode == OpCodes.Leave_S || il.OpCode == OpCodes.Endfinally) &&
                    ilNext != null && ilNext.TryCatchOperations.Any(x => x.Type == MsilTryCatchOperationType.EndExceptionBlock))
                    continue;
                if (il.OpCode == OpCodes.Endfinally && ilNext != null &&
                    ilNext.TryCatchOperations.Any(x => x.Type == MsilTryCatchOperationType.EndExceptionBlock))
                    continue;

                if (il.Operand != null)
                    il.Operand.Emit(target);
                else
                    target.Emit(il.OpCode);
            }
        }

        /// <summary>
        /// Analyzes the integrity of a set of instructions.
        /// </summary>
        /// <param name="level">default logging level</param>
        /// <param name="instructions">instructions</param>
        public static void IntegrityAnalysis(PatchUtilities.DelPrintIntegrityInfo log, IReadOnlyList<MsilInstruction> instructions, bool offests = false)
        {
            var targets = new Dictionary<MsilLabel, int>();
            for (var i = 0; i < instructions.Count; i++)
                foreach (var label in instructions[i].Labels)
                {
                    if (targets.TryGetValue(label, out var other))
                        _log.Warn($"Label {label} is applied to ({i}: {instructions[i]}) and ({other}: {instructions[other]})");
                    targets[label] = i;
                }

            var simpleLabelNames = new Dictionary<MsilLabel, string>();
            foreach (var lbl in targets.OrderBy(x => x.Value))
                simpleLabelNames.Add(lbl.Key, "L" + simpleLabelNames.Count);

            var reparsed = new HashSet<MsilLabel>();
            var labelStackSize = new Dictionary<MsilLabel, Dictionary<int, int>>();
            var stack = 0;
            var unreachable = false;
            var data = new StringBuilder[instructions.Count];
            var tryCatchDepth = new int[instructions.Count];

            for (var i = 0; i < instructions.Count - 1; i++)
            {
                var k = instructions[i];
                var prevDepth = i > 0 ? tryCatchDepth[i] : 0;
                var currentDepth = prevDepth;
                foreach (var tro in k.TryCatchOperations)
                    if (tro.Type == MsilTryCatchOperationType.BeginExceptionBlock)
                        currentDepth++;
                    else if (tro.Type == MsilTryCatchOperationType.EndExceptionBlock)
                        currentDepth--;
                tryCatchDepth[i + 1] = currentDepth;
            }

            for (var i = 0; i < instructions.Count; i++)
            {
                var tryCatchDepthSelf = tryCatchDepth[i];
                var k = instructions[i];
                var line = (data[i] ?? (data[i] = new StringBuilder())).Clear();
                foreach (var tro in k.TryCatchOperations)
                {
                    if (tro.Type == MsilTryCatchOperationType.BeginExceptionBlock)
                        tryCatchDepthSelf++;
                    line.AppendLine($"{new string(' ', (tryCatchDepthSelf - 1) * 2)}// {tro.Type} ({tro.CatchType}) ({tro.NativeOffset:X4})");
                    if (tro.Type == MsilTryCatchOperationType.EndExceptionBlock)
                        tryCatchDepthSelf--;
                }

                var tryCatchIndent = new string(' ', tryCatchDepthSelf * 2);

                if (!unreachable)
                {
                    foreach (var label in k.Labels)
                    {
                        if (!labelStackSize.TryGetValue(label, out Dictionary<int, int> otherStack))
                            labelStackSize[label] = otherStack = new Dictionary<int, int>();

                        otherStack[i - 1] = stack;
                        if (otherStack.Values.Distinct().Count() > 1 || (otherStack.Count == 1 && !otherStack.ContainsValue(stack)))
                        {
                            string otherDesc = string.Join(", ", otherStack.Select(x => $"{x.Key:X4}=>{x.Value}"));
                            line.AppendLine($"WARN{tryCatchIndent}// | Label {simpleLabelNames[label]} has multiple entry stack sizes ({otherDesc})");
                        }
                    }
                }

                foreach (var label in k.Labels)
                {
                    if (!labelStackSize.TryGetValue(label, out var entry))
                    {
                        line.AppendLine($"{tryCatchIndent}// \\/ Label {simpleLabelNames[label]}");
                        continue;
                    }
                    string desc = string.Join(", ", entry.Select(x => $"{x.Key:X4}=>{x.Value}"));
                    line.AppendLine($"{tryCatchIndent}// \\/ Label {simpleLabelNames[label]} has stack sizes {desc}");
                    if (unreachable && entry.Any())
                    {
                        stack = entry.Values.First();
                        unreachable = false;
                    }
                }

                if (k.TryCatchOperations.Any(x => x.Type == MsilTryCatchOperationType.BeginClauseBlock))
                    stack++; // Exception info
                stack += k.StackChange();
                line.Append($"{tryCatchIndent}{(offests ? k.Offset : i):X4} S:{stack:D2} dS:{k.StackChange():+0;-#}\t{k.OpCode}\t");
                if (k.Operand is MsilOperandBrTarget bri)
                    line.Append(simpleLabelNames[bri.Target]);
                else
                    line.Append(k.Operand);
                line.AppendLine($"\t{(unreachable ? "\t// UNREACHABLE" : "")}");
                MsilLabel[] branchTargets = null;
                if (k.Operand is MsilOperandBrTarget br)
                    branchTargets = new[] {br.Target};
                else if (k.Operand is MsilOperandSwitch swi)
                    branchTargets = swi.Labels;

                if (branchTargets != null)
                {
                    var foundUnprocessed = false;
                    foreach (var brTarget in branchTargets)
                    {
                        if (!labelStackSize.TryGetValue(brTarget, out Dictionary<int, int> otherStack))
                            labelStackSize[brTarget] = otherStack = new Dictionary<int, int>();

                        otherStack[i] = stack;
                        if (otherStack.Values.Distinct().Count() > 1 || (otherStack.Count == 1 && !otherStack.ContainsValue(stack)))
                        {
                            string otherDesc = string.Join(", ", otherStack.Select(x => $"{x.Key:X4}=>{x.Value}"));
                            line.AppendLine($"WARN{tryCatchIndent}// ^ Label {simpleLabelNames[brTarget]} has multiple entry stack sizes ({otherDesc})");
                        }

                        if (targets.TryGetValue(brTarget, out var target) && target < i && reparsed.Add(brTarget))
                        {
                            i = target - 1;
                            unreachable = false;
                            foundUnprocessed = true;
                            break;
                        }
                    }

                    if (foundUnprocessed)
                        continue;
                }

                if (k.OpCode == OpCodes.Br || k.OpCode == OpCodes.Br_S || k.OpCode == OpCodes.Leave || k.OpCode == OpCodes.Leave_S)
                    unreachable = true;
            }

            foreach (var k in data)
            foreach (var line in k.ToString().Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.StartsWith("WARN", StringComparison.OrdinalIgnoreCase))
                    log(true, line.Substring(4).Trim());
                else
                    log(false, line.Trim('\n', '\r'));
            }
        }

        private static IEnumerable<MsilInstruction> FixBranchAndReturn(IEnumerable<MsilInstruction> insn, MsilLabel retTarget)
        {
            foreach (MsilInstruction i in insn)
            {
                if (retTarget != null && i.OpCode == OpCodes.Ret)
                {
                    var j = i.CopyWith(OpCodes.Br).InlineTarget(retTarget);
                    _log.Trace($"Replacing {i} with {j}");
                    yield return j;
                }
                else if (_shortToLongBranch.TryGetValue(i.OpCode, out OpCode replaceOpcode))
                {
                    var result = i.CopyWith(replaceOpcode);
                    _log.Trace($"Replacing {i} with {result}");
                    yield return result;
                }
                else
                    yield return i;
            }
        }

        private static readonly Dictionary<OpCode, OpCode> _shortToLongBranch;
        private static readonly Dictionary<OpCode, OpCode> _longToShortBranch;

        static MethodTranspiler()
        {
            _shortToLongBranch = new Dictionary<OpCode, OpCode>();
            _longToShortBranch = new Dictionary<OpCode, OpCode>();
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var opcode = (OpCode) field.GetValue(null);
                if (opcode.OperandType == OperandType.ShortInlineBrTarget &&
                    opcode.Name.EndsWith(".s", StringComparison.OrdinalIgnoreCase))
                {
                    var other = (OpCode?) typeof(OpCodes).GetField(field.Name.Substring(0, field.Name.Length - 2),
                        BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
                    if (other.HasValue && other.Value.OperandType == OperandType.InlineBrTarget)
                    {
                        _shortToLongBranch.Add(opcode, other.Value);
                        _longToShortBranch.Add(other.Value, opcode);
                    }
                }
            }

            _shortToLongBranch[OpCodes.Leave_S] = OpCodes.Leave;
            _longToShortBranch[OpCodes.Leave] = OpCodes.Leave_S;
        }
    }
}