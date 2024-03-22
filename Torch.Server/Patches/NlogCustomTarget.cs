using System;
using System.Windows.Forms;
using NLog;
using NLog.Targets;

namespace Torch.Patches
{
    [Target("NlogCustomTarget")]
    public class NlogCustomTarget : TargetWithLayout
    {
        public static event Action<LogEventInfo> LogEventReceived;

        protected override void Write(LogEventInfo logEvent)
        {
            LogEventReceived?.Invoke(logEvent);
        }
    }
}