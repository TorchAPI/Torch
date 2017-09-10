using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Managers.PatchManager.Transpile
{
    internal class MethodTranspiler
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal static void Transpile(MethodBase baseMethod, IEnumerable<MethodInfo> transpilers, LoggingIlGenerator output, Label? retLabel)
        {
            var context = new MethodContext(baseMethod);
            context.Read();
            _log.Trace("Input Method:");
            _log.Trace(context.ToHumanMsil);

            var methodContent = (IEnumerable<MsilInstruction>) context.Instructions;
            foreach (var transpiler in transpilers)
                methodContent = (IEnumerable<MsilInstruction>)transpiler.Invoke(null, new object[] { methodContent });
            methodContent = FixBranchAndReturn(methodContent, retLabel);
            foreach (var k in methodContent)
                k.Emit(output);
        }

        private static IEnumerable<MsilInstruction> FixBranchAndReturn(IEnumerable<MsilInstruction> insn, Label? retTarget)
        {
            foreach (var i in insn)
            {
                if (retTarget.HasValue && i.OpCode == OpCodes.Ret)
                {
                    var j = new MsilInstruction(OpCodes.Br);
                    ((MsilOperandBrTarget)j.Operand).Target = new MsilLabel(retTarget.Value);
                    yield return j;
                    continue;
                }
                if (_opcodeReplaceRule.TryGetValue(i.OpCode, out OpCode replaceOpcode))
                {
                    yield return new MsilInstruction(replaceOpcode) { Operand = i.Operand };
                    continue;
                }
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
                    var other = (OpCode?) typeof(OpCodes).GetField(field.Name.Substring(0, field.Name.Length - 2),
                        BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
                    if (other.HasValue && other.Value.OperandType == OperandType.InlineBrTarget)
                        _opcodeReplaceRule.Add(opcode, other.Value);
                }
            }
        }
    }
}
