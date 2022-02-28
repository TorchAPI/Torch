using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.Utils;

namespace Torch.Patches
{
    [PatchShim]
    internal static class KeenLogPatch
    {
        private static readonly Logger _log = LogManager.GetLogger("Keen");

#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.Log), Parameters = new[] { typeof(MyLogSeverity), typeof(StringBuilder) })]
        private static MethodInfo _logStringBuilder;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.Log), Parameters = new[] { typeof(MyLogSeverity), typeof(string), typeof(object[]) })]
        private static MethodInfo _logFormatted;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.WriteLine), Parameters = new[] { typeof(string) })]
        private static MethodInfo _logWriteLine;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.AppendToClosedLog), Parameters = new[] { typeof(string) })]
        private static MethodInfo _logAppendToClosedLog;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.WriteLine), Parameters = new[] { typeof(string), typeof(LoggingOptions) })]
        private static MethodInfo _logWriteLineOptions;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.WriteLine), Parameters = new[] { typeof(Exception) })]
        private static MethodInfo _logWriteLineException;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.AppendToClosedLog), Parameters = new[] { typeof(Exception) })]
        private static MethodInfo _logAppendToClosedLogException;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.WriteLineAndConsole), Parameters = new[] { typeof(string) })]
        private static MethodInfo _logWriteLineAndConsole;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.Init))]
        private static MethodInfo _logInit;

        [ReflectedMethodInfo(typeof(MyLog), nameof(MyLog.Close))]
        private static MethodInfo _logClose;
#pragma warning restore 649
        

        public static void Patch(PatchContext context)
        {
            context.GetPattern(_logStringBuilder).AddPrefix(nameof(PrefixLogStringBuilder));
            context.GetPattern(_logFormatted).AddPrefix(nameof(PrefixLogFormatted));

            context.GetPattern(_logWriteLine).AddPrefix(nameof(PrefixWriteLine));
            context.GetPattern(_logAppendToClosedLog).AddPrefix(nameof(PrefixAppendToClosedLog));
            context.GetPattern(_logWriteLineAndConsole).AddPrefix(nameof(PrefixWriteLineConsole));

            context.GetPattern(_logWriteLineException).AddPrefix(nameof(PrefixWriteLineException));
            context.GetPattern(_logAppendToClosedLogException).AddPrefix(nameof(PrefixAppendToClosedLogException));

            context.GetPattern(_logWriteLineOptions).AddPrefix(nameof(PrefixWriteLineOptions));
            
            context.GetPattern(_logInit).AddPrefix(nameof(PrefixInit));
            context.GetPattern(_logClose).AddPrefix(nameof(PrefixClose));
        }

        [ReflectedMethod(Name = "GetIdentByThread")]
        private static Func<MyLog, int, int> _getIndentByThread = null!;

        [ReflectedGetter(Name = "m_lock")]
        private static Func<MyLog, FastResourceLock> _lockGetter = null!;

        [ReflectedSetter(Name = "m_enabled")]
        private static Action<MyLog, bool> _enabledSetter = null!;

        private static int GetIndentByCurrentThread()
        {
            using var l = _lockGetter(MyLog.Default).AcquireExclusiveUsing();
            return _getIndentByThread(MyLog.Default, Environment.CurrentManagedThreadId);
        }

        private static bool PrefixClose() => false;

        private static bool PrefixInit(MyLog __instance, StringBuilder appVersionString)
        {
            __instance.WriteLine("Log Started");
            var byThreadField =
                typeof(MyLog).GetField("m_indentsByThread", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var indentsField = typeof(MyLog).GetField("m_indents", BindingFlags.Instance | BindingFlags.NonPublic)!;
            
            byThreadField.SetValue(__instance, Activator.CreateInstance(byThreadField.FieldType));
            indentsField.SetValue(__instance, Activator.CreateInstance(indentsField.FieldType));
            _enabledSetter(__instance, true);
            return false;
        }

        private static bool PrefixWriteLine(MyLog __instance, string msg)
        {
            if (__instance.LogEnabled && _log.IsDebugEnabled)
                _log.Debug($"{string.Empty.PadRight(3 * GetIndentByCurrentThread(), ' ')}{msg}");
            return false;
        }

        private static bool PrefixWriteLineConsole(MyLog __instance, string msg)
        {
            if (__instance.LogEnabled && _log.IsInfoEnabled)
                _log.Info($"{string.Empty.PadRight(3 * GetIndentByCurrentThread(), ' ')}{msg}");
            return false;
        }

        private static bool PrefixAppendToClosedLog(MyLog __instance, string text)
        {
            if (__instance.LogEnabled && _log.IsDebugEnabled)
                _log.Debug($"{string.Empty.PadRight(3 * GetIndentByCurrentThread(), ' ')}{text}");
            return false;
        }
        private static bool PrefixWriteLineOptions(MyLog __instance, string message, LoggingOptions option)
        {
            if (__instance.LogEnabled && __instance.LogFlag(option) && _log.IsDebugEnabled)
                _log.Info($"{string.Empty.PadRight(3 * GetIndentByCurrentThread(), ' ')}{message}");
            return false;
        }

        private static bool PrefixAppendToClosedLogException(Exception e)
        {
            _log.Error(e);
            return false;
        }

        private static bool PrefixWriteLineException(Exception ex)
        {
            _log.Error(ex);
            return false;
        }

        private static bool PrefixLogFormatted(MyLog __instance, MyLogSeverity severity, string format, object[] args)
        {
            if (__instance.LogEnabled)
                return false;

            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            _log.Log(new(LogLevelFor(severity), _log.Name, $"{string.Empty.PadRight(3 * GetIndentByCurrentThread(), ' ')}{string.Format(format, args)}"));
            return false;
        }

        private static bool PrefixLogStringBuilder(MyLog __instance, MyLogSeverity severity, StringBuilder builder)
        {
            if (!__instance.LogEnabled) return false;
            var indent = GetIndentByCurrentThread() * 3;
                
            // because append resizes every char
            builder.EnsureCapacity(indent);
            builder.Append(' ', indent);
                
            _log.Log(LogLevelFor(severity), builder);
            return false;
        }

        private static LogLevel LogLevelFor(MyLogSeverity severity)
        {
            return severity switch
            {
                MyLogSeverity.Debug => LogLevel.Debug,
                MyLogSeverity.Info => LogLevel.Info,
                MyLogSeverity.Warning => LogLevel.Warn,
                MyLogSeverity.Error => LogLevel.Error,
                MyLogSeverity.Critical => LogLevel.Fatal,
                _ => LogLevel.Info
            };
        }
    }
}