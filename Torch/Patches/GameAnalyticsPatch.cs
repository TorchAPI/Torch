using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Config;
using Torch.Managers.PatchManager;

namespace Torch.Patches
{
    [PatchShim]
    public static class GameAnalyticsPatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static Action<ILogger> _setLogger;

        public static void Patch(PatchContext ctx)
        {
            Type type = Type.GetType("GameAnalyticsSDK.Net.Logging.GALogger, GameAnalytics.Mono");
            if (type == null)
                return;
            FieldInfo loggerField = type.GetField("logger",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (loggerField == null)
            {
                _log.Warn("GALogger logger field is unknown.  Logging may not function.");
                return;
            }
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            _setLogger = loggerField?.CreateSetter<ILogger>();
            FixLogging();

            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);
            if (ctor == null)
            {
                _log.Warn("GALogger constructor is unknown.  Logging may not function.");
                return;
            }
            ctx.GetPattern(ctor).Prefixes.Add(typeof(GameAnalyticsPatch).GetMethod(nameof(PatchLogger),
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
        }

        private static void FixLogging()
        {
            _setLogger(LogManager.GetLogger("GameAnalytics"));
            if (!(LogManager.Configuration is XmlLoggingConfiguration))
                LogManager.Configuration = new XmlLoggingConfiguration(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? Environment.CurrentDirectory, "NLog.config"));
        }

        private static bool PatchLogger()
        {
            FixLogging();
            return false;
        }
    }
}
