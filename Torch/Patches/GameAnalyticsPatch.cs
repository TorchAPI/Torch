using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Config;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace Torch.Patches
{
    [PatchShim]
    public static class GameAnalyticsPatch
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static Action<ILogger, ILogger> _setLogger;

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
            _setLogger = loggerField?.CreateSetter<ILogger, ILogger>();
            FixLogging();

            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);
            if (ctor == null)
            {
                _log.Warn("GALogger constructor is unknown.  Logging may not function.");
                return;
            }
            ctx.GetPattern(ctor).AddPrefix(nameof(PatchLogger));
        }

        private static void FixLogging()
        {
            TorchLogManager.RestoreGlobalConfiguration();
            _setLogger(null, LogManager.GetLogger("GameAnalytics"));
        }

        private static bool PatchLogger()
        {
            FixLogging();
            return false;
        }
    }
}
