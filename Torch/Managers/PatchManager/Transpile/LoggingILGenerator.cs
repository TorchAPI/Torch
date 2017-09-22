using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Torch.Utils;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162 // unreachable code
namespace Torch.Managers.PatchManager.Transpile
{
    /// <summary>
    /// An ILGenerator that can log emit calls when the TRACE level is enabled.
    /// </summary>
    public class LoggingIlGenerator
    {
        private const int _opcodePadding = -10;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Backing generator
        /// </summary>
        public ILGenerator Backing { get; }

        /// <summary>
        /// Creates a new logging IL generator backed by the given generator.
        /// </summary>
        /// <param name="backing">Backing generator</param>
        public LoggingIlGenerator(ILGenerator backing)
        {
            Backing = backing;
        }

        /// <inheritdoc cref="ILGenerator.DeclareLocal(Type, bool)"/>
        public LocalBuilder DeclareLocal(Type localType, bool isPinned = false)
        {
            LocalBuilder res = Backing.DeclareLocal(localType, isPinned);
            _log?.Trace($"DclLoc\t{res.LocalIndex}\t=> {res.LocalType} {res.IsPinned}");
            return res;
        }


        /// <inheritdoc cref="ILGenerator.Emit(OpCode)"/>
        public void Emit(OpCode op)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding}");
            Backing.Emit(op);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, LocalBuilder)"/>
        public void Emit(OpCode op, LocalBuilder arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} Local:{arg.LocalIndex}/{arg.LocalType}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, int)"/>
        public void Emit(OpCode op, int arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, long)"/>
        public void Emit(OpCode op, long arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, float)"/>
        public void Emit(OpCode op, float arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, double)"/>
        public void Emit(OpCode op, double arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, string)"/>
        public void Emit(OpCode op, string arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Type)"/>
        public void Emit(OpCode op, Type arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, FieldInfo)"/>
        public void Emit(OpCode op, FieldInfo arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, MethodInfo)"/>
        public void Emit(OpCode op, MethodInfo arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }


#pragma warning disable 649
        [ReflectedGetter(Name="m_label")]
        private static Func<Label, int> _labelID;
#pragma warning restore 649

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label)"/>
        public void Emit(OpCode op, Label arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding}\tL:{_labelID.Invoke(arg)}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label[])"/>
        public void Emit(OpCode op, Label[] arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding}\t{string.Join(", ", arg.Select(x => "L:" + _labelID.Invoke(x)))}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, SignatureHelper)"/>
        public void Emit(OpCode op, SignatureHelper arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, ConstructorInfo)"/>
        public void Emit(OpCode op, ConstructorInfo arg)
        {
            _log?.Trace($"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.MarkLabel(Label)"/>
        public void MarkLabel(Label label)
        {
            _log?.Trace($"MkLbl\tL:{_labelID.Invoke(label)}");
            Backing.MarkLabel(label);
        }

        /// <inheritdoc cref="ILGenerator.DefineLabel()"/>
        public Label DefineLabel()
        {
            return Backing.DefineLabel();
        }

        /// <summary>
        /// Emits a comment to the log.
        /// </summary>
        /// <param name="comment">Comment</param>
        [Conditional("DEBUG")]
        public void EmitComment(string comment)
        {
            _log?.Trace($"// {comment}");
        }
    }
#pragma warning restore 162
}
