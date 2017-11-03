using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Documents;
using NLog;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Managers.PatchManager.Transpile
{
    internal class MethodTranspiler
    {
        public static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal static void Transpile(MethodBase baseMethod, Func<Type, MsilLocal> localCreator,
            IEnumerable<MethodInfo> transpilers, LoggingIlGenerator output, Label? retLabel,
            bool logMsil)
        {
            var context = new MethodContext(baseMethod);
            context.Read();
            // IntegrityAnalysis(LogLevel.Trace, context.Instructions);

            var methodContent = (IEnumerable<MsilInstruction>)context.Instructions;
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
                methodContent = (IEnumerable<MsilInstruction>)transpiler.Invoke(null, paramList.ToArray());
            }
            methodContent = FixBranchAndReturn(methodContent, retLabel);
            var list = methodContent.ToList();
            if (logMsil)
            {
                IntegrityAnalysis(LogLevel.Info, list);
            }
            EmitMethod(list, output);
        }

        internal static void EmitMethod(IReadOnlyList<MsilInstruction> instructions, LoggingIlGenerator target)
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                MsilInstruction il = instructions[i];
                if (il.TryCatchOperation != null)
                    switch (il.TryCatchOperation.Type)
                    {
                        case MsilTryCatchOperationType.BeginExceptionBlock:
                            target.BeginExceptionBlock();
                            break;
                        case MsilTryCatchOperationType.BeginCatchBlock:
                            target.BeginCatchBlock(il.TryCatchOperation.CatchType);
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

                MsilInstruction ilNext = i < instructions.Count - 1 ? instructions[i + 1] : null;

                // Leave opcodes emitted by these:
                if (il.OpCode == OpCodes.Endfilter && ilNext?.TryCatchOperation?.Type ==
                    MsilTryCatchOperationType.BeginCatchBlock)
                    continue;
                if ((il.OpCode == OpCodes.Leave || il.OpCode == OpCodes.Leave_S) &&
                    (ilNext?.TryCatchOperation?.Type == MsilTryCatchOperationType.EndExceptionBlock ||
                     ilNext?.TryCatchOperation?.Type == MsilTryCatchOperationType.BeginCatchBlock ||
                     ilNext?.TryCatchOperation?.Type == MsilTryCatchOperationType.BeginFinallyBlock))
                    continue;
                if ((il.OpCode == OpCodes.Leave || il.OpCode == OpCodes.Leave_S) &&
                    ilNext?.TryCatchOperation?.Type == MsilTryCatchOperationType.EndExceptionBlock)
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
        private static void IntegrityAnalysis(LogLevel level, IReadOnlyList<MsilInstruction> instructions)
        {
            var targets = new Dictionary<MsilLabel, int>();
            for (var i = 0; i < instructions.Count; i++)
                foreach (var label in instructions[i].Labels)
                {
                    if (targets.TryGetValue(label, out var other))
                        _log.Warn($"Label {label} is applied to ({i}: {instructions[i]}) and ({other}: {instructions[other]})");
                    targets[label] = i;
                }

            var reparsed = new HashSet<MsilLabel>();
            var labelStackSize = new Dictionary<MsilLabel, Dictionary<int, int>>();
            var stack = 0;
            var unreachable = false;
            var data = new StringBuilder[instructions.Count];
            for (var i = 0; i < instructions.Count; i++)
            {
                var k = instructions[i];
                var line = (data[i] ?? (data[i] = new StringBuilder())).Clear();
                if (!unreachable)
                {
                    foreach (var label in k.Labels)
                    {
                        if (!labelStackSize.TryGetValue(label, out Dictionary<int, int> otherStack))
                            labelStackSize[label] = otherStack = new Dictionary<int, int>();

                        otherStack.Add(i - 1, stack);
                        if (otherStack.Values.Distinct().Count() > 1 || (otherStack.Count == 1 && !otherStack.ContainsValue(stack)))
                        {
                            string otherDesc = string.Join(", ", otherStack.Select(x => $"{x.Key:X4}=>{x.Value}"));
                            line.AppendLine($"WARN// | Label {label} has multiple entry stack sizes ({otherDesc})");
                        }
                    }
                }
                foreach (var label in k.Labels)
                {
                    if (!labelStackSize.TryGetValue(label, out var entry))
                        continue;
                    string desc = string.Join(", ", entry.Select(x => $"{x.Key:X4}=>{x.Value}"));
                    line.AppendLine($"// \\/ Label {label} has stack sizes {desc}");
                    if (unreachable && entry.Any())
                    {
                        stack = entry.Values.First();
                        unreachable = false;
                    }
                }
                stack += k.StackChange();
                if (k.TryCatchOperation != null)
                    line.AppendLine($"// .{k.TryCatchOperation.Type} ({k.TryCatchOperation.CatchType})");
                line.AppendLine($"{i:X4} S:{stack:D2} dS:{k.StackChange():+0;-#}\t{k}" + (unreachable ? "\t// UNREACHABLE" : ""));
                if (k.Operand is MsilOperandBrTarget br)
                {
                    if (!targets.ContainsKey(br.Target))
                        line.AppendLine($"WARN// ^ Unknown target {br.Target}");

                    if (!labelStackSize.TryGetValue(br.Target, out Dictionary<int, int> otherStack))
                        labelStackSize[br.Target] = otherStack = new Dictionary<int, int>();

                    otherStack[i] = stack;
                    if (otherStack.Values.Distinct().Count() > 1 || (otherStack.Count == 1 && !otherStack.ContainsValue(stack)))
                    {
                        string otherDesc = string.Join(", ", otherStack.Select(x => $"{x.Key:X4}=>{x.Value}"));
                        line.AppendLine($"WARN// ^ Label {br.Target} has multiple entry stack sizes ({otherDesc})");
                    }
                    if (targets.TryGetValue(br.Target, out var target) && target < i && reparsed.Add(br.Target))
                    {
                        i = target - 1;
                        unreachable = false;
                        continue;
                    }
                }
                if (k.OpCode == OpCodes.Br || k.OpCode == OpCodes.Br_S)
                    unreachable = true;
            }
            foreach (var k in data)
                foreach (var line in k.ToString().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.StartsWith("WARN", StringComparison.OrdinalIgnoreCase))
                        _log.Warn(line.Substring(4).Trim());
                    else
                        _log.Log(level, line.Trim());
                }
        }

        private static IEnumerable<MsilInstruction> FixBranchAndReturn(IEnumerable<MsilInstruction> insn, Label? retTarget)
        {
            foreach (MsilInstruction i in insn)
            {
                if (retTarget.HasValue && i.OpCode == OpCodes.Ret)
                {
                    var j = i.CopyWith(OpCodes.Br).InlineTarget(new MsilLabel(retTarget.Value));
                    _log.Trace($"Replacing {i} with {j}");
                    yield return j;
                }
                else if (_opcodeReplaceRule.TryGetValue(i.OpCode, out OpCode replaceOpcode))
                {
                    var result = i.CopyWith(replaceOpcode);
                    _log.Trace($"Replacing {i} with {result}");
                    yield return result;
                }
                else
                    yield return i;
            }
        }

        private static readonly Dictionary<OpCode, OpCode> _opcodeReplaceRule;
        static MethodTranspiler()
        {
            _opcodeReplaceRule = new Dictionary<OpCode, OpCode>();
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var opcode = (OpCode)field.GetValue(null);
                if (opcode.OperandType == OperandType.ShortInlineBrTarget &&
                    opcode.Name.EndsWith(".s", StringComparison.OrdinalIgnoreCase))
                {
                    var other = (OpCode?)typeof(OpCodes).GetField(field.Name.Substring(0, field.Name.Length - 2),
                        BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
                    if (other.HasValue && other.Value.OperandType == OperandType.InlineBrTarget)
                        _opcodeReplaceRule.Add(opcode, other.Value);
                }
            }
            _opcodeReplaceRule[OpCodes.Leave_S] = OpCodes.Leave;
        }
    }
}
