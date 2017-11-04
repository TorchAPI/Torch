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

        private readonly LogLevel _level;

        /// <summary>
        /// Creates a new logging IL generator backed by the given generator.
        /// </summary>
        /// <param name="backing">Backing generator</param>
        public LoggingIlGenerator(ILGenerator backing, LogLevel level)
        {
            Backing = backing;
            _level = level;
        }

        /// <inheritdoc cref="ILGenerator.DeclareLocal(Type, bool)"/>
        public LocalBuilder DeclareLocal(Type localType, bool isPinned = false)
        {
            LocalBuilder res = Backing.DeclareLocal(localType, isPinned);
            _log?.Log(_level, $"DclLoc\t{res.LocalIndex}\t=> {res.LocalType} {res.IsPinned}");
            return res;
        }


        /// <inheritdoc cref="ILGenerator.Emit(OpCode)"/>
        public void Emit(OpCode op)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding}");
            Backing.Emit(op);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, LocalBuilder)"/>
        public void Emit(OpCode op, LocalBuilder arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} Local:{arg.LocalIndex}/{arg.LocalType}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, byte)"/>
        public void Emit(OpCode op, byte arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, int)"/>
        public void Emit(OpCode op, short arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, int)"/>
        public void Emit(OpCode op, int arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, long)"/>
        public void Emit(OpCode op, long arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, float)"/>
        public void Emit(OpCode op, float arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, double)"/>
        public void Emit(OpCode op, double arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, string)"/>
        public void Emit(OpCode op, string arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Type)"/>
        public void Emit(OpCode op, Type arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, FieldInfo)"/>
        public void Emit(OpCode op, FieldInfo arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, MethodInfo)"/>
        public void Emit(OpCode op, MethodInfo arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }


#pragma warning disable 649
        [ReflectedGetter(Name = "m_label")]
        private static Func<Label, int> _labelID;
#pragma warning restore 649

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label)"/>
        public void Emit(OpCode op, Label arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding}\tL:{_labelID.Invoke(arg)}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label[])"/>
        public void Emit(OpCode op, Label[] arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding}\t{string.Join(", ", arg.Select(x => "L:" + _labelID.Invoke(x)))}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, SignatureHelper)"/>
        public void Emit(OpCode op, SignatureHelper arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, ConstructorInfo)"/>
        public void Emit(OpCode op, ConstructorInfo arg)
        {
            _log?.Log(_level, $"Emit\t{op,_opcodePadding} {arg}");
            Backing.Emit(op, arg);
        }


        #region Exceptions
        /// <inheritdoc cref="ILGenerator.BeginExceptionBlock"/>
        public Label BeginExceptionBlock()
        {
            _log?.Log(_level, $"BeginExceptionBlock");
            return Backing.BeginExceptionBlock();
        }

        /// <inheritdoc cref="ILGenerator.BeginCatchBlock"/>
        public void BeginCatchBlock(Type caught)
        {
            _log?.Log(_level, $"BeginCatchBlock {caught}");
            Backing.BeginCatchBlock(caught);
        }

        /// <inheritdoc cref="ILGenerator.BeginExceptFilterBlock"/>
        public void BeginExceptFilterBlock()
        {
            _log?.Log(_level, $"BeginExceptFilterBlock");
            Backing.BeginExceptFilterBlock();
        }

        /// <inheritdoc cref="ILGenerator.BeginFaultBlock"/>
        public void BeginFaultBlock()
        {
            _log?.Log(_level, $"BeginFaultBlock");
            Backing.BeginFaultBlock();
        }

        /// <inheritdoc cref="ILGenerator.BeginFinallyBlock"/>
        public void BeginFinallyBlock()
        {
            _log?.Log(_level, $"BeginFinallyBlock");
            Backing.BeginFinallyBlock();
        }

        /// <inheritdoc cref="ILGenerator.EndExceptionBlock"/>
        public void EndExceptionBlock()
        {
            _log?.Log(_level, $"EndExceptionBlock");
            Backing.EndExceptionBlock();
        }
        #endregion

        /// <inheritdoc cref="ILGenerator.MarkLabel(Label)"/>
        public void MarkLabel(Label label)
        {
            _log?.Log(_level, $"MkLbl\tL:{_labelID.Invoke(label)}");
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
            _log?.Log(_level, $"// {comment}");
        }
    }
#pragma warning restore 162
}
