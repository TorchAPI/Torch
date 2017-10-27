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
using VRage.Utils;

namespace Torch.Managers
{
    [PatchShim]
    internal class KeenLogManager
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
#pragma warning restore 649
        

        public static void Patch(PatchContext context)
        {
            context.GetPattern(_logStringBuilder).Prefixes.Add(Method(nameof(PrefixLogStringBuilder)));
            context.GetPattern(_logFormatted).Prefixes.Add(Method(nameof(PrefixLogFormatted)));

            context.GetPattern(_logWriteLine).Prefixes.Add(Method(nameof(PrefixWriteLine)));
            context.GetPattern(_logAppendToClosedLog).Prefixes.Add(Method(nameof(PrefixAppendToClosedLog)));
            context.GetPattern(_logWriteLineAndConsole).Prefixes.Add(Method(nameof(PrefixWriteLineConsole)));

            context.GetPattern(_logWriteLineException).Prefixes.Add(Method(nameof(PrefixWriteLineException)));
            context.GetPattern(_logAppendToClosedLogException).Prefixes.Add(Method(nameof(PrefixAppendToClosedLogException)));

            context.GetPattern(_logWriteLineOptions).Prefixes.Add(Method(nameof(PrefixWriteLineOptions)));
            
        }

        private static MethodInfo Method(string name)
        {
            return typeof(KeenLogManager).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        [ReflectedMethod(Name = "GetThreadId")]
        private static Func<MyLog, int> _getThreadId;

        [ReflectedMethod(Name = "GetIdentByThread")]
        private static Func<MyLog, int, int> _getIndentByThread;

        private static readonly ThreadLocal<StringBuilder> _tmpStringBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(32));

        private static StringBuilder PrepareLog(MyLog log)
        {
            return _tmpStringBuilder.Value.Clear().Append(' ', _getIndentByThread(log, _getThreadId(log)) * 3);
        }

        private static bool PrefixWriteLine(MyLog __instance, string msg)
        {
            _log.Debug(PrepareLog(__instance).Append(msg));
            return false;
        }

        private static bool PrefixWriteLineConsole(MyLog __instance, string msg)
        {
            _log.Info(PrepareLog(__instance).Append(msg));
            return false;
        }

        private static bool PrefixAppendToClosedLog(MyLog __instance, string text)
        {
            _log.Info(PrepareLog(__instance).Append(text));
            return false;
        }
        private static bool PrefixWriteLineOptions(MyLog __instance, string message, LoggingOptions option)
        {
            if (__instance.LogFlag(option))
                _log.Info(PrepareLog(__instance).Append(message));
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
            _log.Log(LogLevelFor(severity), PrepareLog(__instance).AppendFormat(format, args));
            return false;
        }

        private static bool PrefixLogStringBuilder(MyLog __instance, MyLogSeverity severity, StringBuilder builder)
        {
            _log.Log(LogLevelFor(severity), PrepareLog(__instance).Append(builder));
            return false;
        }

        private static LogLevel LogLevelFor(MyLogSeverity severity)
        {
            switch (severity)
            {
                case MyLogSeverity.Debug:
                    return LogLevel.Debug;
                case MyLogSeverity.Info:
                    return LogLevel.Info;
                case MyLogSeverity.Warning:
                    return LogLevel.Warn;
                case MyLogSeverity.Error:
                    return LogLevel.Error;
                case MyLogSeverity.Critical:
                    return LogLevel.Fatal;
                default:
                    return LogLevel.Info;
            }
        }
    }
}
