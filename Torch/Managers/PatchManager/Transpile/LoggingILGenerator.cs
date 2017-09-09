using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NLog;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162 // unreachable code
namespace Torch.Managers.PatchManager.Transpile
{
    /// <summary>
    /// An ILGenerator that can log emit calls when <see cref="LoggingIlGenerator._logging"/> is enabled.
    /// </summary>
    public class LoggingIlGenerator
    {
        private const bool _logging = false;
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
            if (_logging)
                _log.Trace($"DeclareLocal {res.LocalType} {res.IsPinned} => {res.LocalIndex}");
            return res;
        }


        /// <inheritdoc cref="ILGenerator.Emit(OpCode)"/>
        public void Emit(OpCode op)
        {
            if (_logging)
                _log.Trace($"Emit {op}");
            Backing.Emit(op);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, LocalBuilder)"/>
        public void Emit(OpCode op, LocalBuilder arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} L:{arg.LocalIndex} {arg.LocalType}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, int)"/>
        public void Emit(OpCode op, int arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, long)"/>
        public void Emit(OpCode op, long arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, float)"/>
        public void Emit(OpCode op, float arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, double)"/>
        public void Emit(OpCode op, double arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, string)"/>
        public void Emit(OpCode op, string arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Type)"/>
        public void Emit(OpCode op, Type arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, FieldInfo)"/>
        public void Emit(OpCode op, FieldInfo arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, MethodInfo)"/>
        public void Emit(OpCode op, MethodInfo arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        private static FieldInfo _labelID =
            typeof(Label).GetField("m_label", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label)"/>
        public void Emit(OpCode op, Label arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} L:{_labelID.GetValue(arg)}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, Label[])"/>
        public void Emit(OpCode op, Label[] arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {string.Join(", ", arg.Select(x => "L:" + _labelID.GetValue(x)))}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, SignatureHelper)"/>
        public void Emit(OpCode op, SignatureHelper arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.Emit(OpCode, ConstructorInfo)"/>
        public void Emit(OpCode op, ConstructorInfo arg)
        {
            if (_logging)
                _log.Trace($"Emit {op} {arg}");
            Backing.Emit(op, arg);
        }

        /// <inheritdoc cref="ILGenerator.MarkLabel(Label)"/>
        public void MarkLabel(Label label)
        {
            if (_logging)
                _log.Trace($"MarkLabel L:{_labelID.GetValue(label)}");
            Backing.MarkLabel(label);
        }

        /// <inheritdoc cref="ILGenerator.DefineLabel()"/>
        public Label DefineLabel()
        {
            return Backing.DefineLabel();
        }
    }
#pragma warning restore 162
}
