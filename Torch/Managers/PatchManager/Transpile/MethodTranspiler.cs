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

        internal static void Transpile(MethodBase baseMethod, Func<Type, MsilLocal> localCreator,
            IEnumerable<MethodInfo> transpilers, LoggingIlGenerator output, Label? retLabel,
            bool logMsil)
        {
            var context = new MethodContext(baseMethod);
            context.Read();
            context.CheckIntegrity();
            //            _log.Trace("Input Method:");
            //            _log.Trace(context.ToHumanMsil);

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
            if (logMsil)
            {
                var list = methodContent.ToList();
                IntegrityAnalysis(list);
                foreach (var k in list)
                    k.Emit(output);
            }
            else
            {
                foreach (var k in methodContent)
                    k.Emit(output);
            }
        }

        /// <summary>
        /// Analyzes the integrity of a set of instructions.
        /// </summary>
        /// <param name="instructions">instructions</param>
        private static void IntegrityAnalysis(List<MsilInstruction> instructions)
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
                        _log.Info(line.Trim());
                }
        }

        internal static void Emit(IEnumerable<MsilInstruction> input, LoggingIlGenerator output)
        {
            foreach (MsilInstruction k in FixBranchAndReturn(input, null))
                k.Emit(output);
        }

        private static IEnumerable<MsilInstruction> FixBranchAndReturn(IEnumerable<MsilInstruction> insn, Label? retTarget)
        {
            foreach (MsilInstruction i in insn)
            {
                if (retTarget.HasValue && i.OpCode == OpCodes.Ret)
                {
                    MsilInstruction j = new MsilInstruction(OpCodes.Br).InlineTarget(new MsilLabel(retTarget.Value));
                    foreach (MsilLabel l in i.Labels)
                        j.Labels.Add(l);
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
