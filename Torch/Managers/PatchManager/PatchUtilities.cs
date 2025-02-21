using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Torch.Managers.PatchManager.MSIL;
using Torch.Managers.PatchManager.Transpile;
using Torch.Utils;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Functions that let you read and write MSIL to methods directly.
    /// </summary>
    public class PatchUtilities
    {
        /// <summary>
        /// Gets the content of a method as an instruction stream
        /// </summary>
        /// <param name="method">Method to examine</param>
        /// <returns>instruction stream</returns>
        public static IEnumerable<MsilInstruction> ReadInstructions(MethodBase method)
        {
            var context = new MethodContext(method);
            context.Read();
            return context.Instructions;
        }

        /// <summary>
        /// Writes the given instruction stream to the given IL generator, fixing short branch instructions.
        /// </summary>
        /// <param name="insn">Instruction stream</param>
        /// <param name="generator">Output</param>
        public static void EmitInstructions(IEnumerable<MsilInstruction> insn, LoggingIlGenerator generator)
        {
            MethodTranspiler.EmitMethod(insn.ToList(), generator);
        }

        public delegate void DelPrintIntegrityInfo(bool error, string msg);

        /// <summary>
        /// Analyzes the integrity of a set of instructions.
        /// </summary>
        /// <param name="handler">Logger</param>
        /// <param name="instructions">instructions</param>
        public static void IntegrityAnalysis(DelPrintIntegrityInfo handler, IReadOnlyList<MsilInstruction> instructions)
        {
            MethodTranspiler.IntegrityAnalysis(handler, instructions);
        }

#pragma warning disable 649
        [ReflectedStaticMethod(Type = typeof(RuntimeHelpers), Name = "_CompileMethod", OverrideTypeNames = new[] {"System.IRuntimeMethodInfo"})]
        private static Action<object> _compileDynamicMethod;

        [ReflectedMethod(Name = "GetMethodInfo")]
        private static Func<RuntimeMethodHandle, object> _getMethodInfo;

        [ReflectedMethod(Name = "GetMethodDescriptor")]
        private static Func<DynamicMethod, RuntimeMethodHandle> _getMethodHandle;
#pragma warning restore 649
        /// <summary>
        /// Forces the given dynamic method to be compiled
        /// </summary>
        /// <param name="method"></param>
        public static void Compile(DynamicMethod method)
        {
            // Force it to compile
            RuntimeMethodHandle handle = _getMethodHandle.Invoke(method);
            object runtimeMethodInfo = _getMethodInfo.Invoke(handle);
            _compileDynamicMethod.Invoke(runtimeMethodInfo);
        }
    }
}