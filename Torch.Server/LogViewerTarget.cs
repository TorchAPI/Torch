using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using NLog;
using NLog.Targets;
using Torch.Server.ViewModels;
using Torch.Server.Views;

namespace Torch.Server
{
    /// <summary>
    /// NLog target that writes to a <see cref="LogViewerControl"/>.
    /// </summary>
    [Target("logViewer")]
    public sealed class LogViewerTarget : TargetWithLayout
    {
        public IList<LogEntry> LogEntries { get; set; }
        public SynchronizationContext TargetContext { get; set; }
        private const int MAX_LINES = 1000;

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {
            TargetContext?.Post(_sendOrPostCallback, logEvent);
        }

        private void WriteCallback(object state)
        {
            var logEvent = (LogEventInfo) state;
            LogEntries?.Add(new(logEvent.TimeStamp, Layout.Render(logEvent), LogLevelColors[logEvent.Level]));
            if (LogEntries is not {Count: > MAX_LINES}) return;
            for (var i = 0; LogEntries.Count > MAX_LINES; i++)
            {
                LogEntries.RemoveAt(i);
            }
        }

        private static readonly Dictionary<LogLevel, SolidColorBrush> LogLevelColors = new()
        {
            [LogLevel.Trace] = new SolidColorBrush(Colors.DimGray),
            [LogLevel.Debug] = new SolidColorBrush(Colors.DarkGray),
            [LogLevel.Info] = new SolidColorBrush(Colors.White),
            [LogLevel.Warn] = new SolidColorBrush(Colors.Magenta),
            [LogLevel.Error] = new SolidColorBrush(Colors.Yellow),
            [LogLevel.Fatal] = new SolidColorBrush(Colors.Red),
        };

        private readonly SendOrPostCallback _sendOrPostCallback;

        public LogViewerTarget()
        {
            _sendOrPostCallback = WriteCallback;
        }
    }
}
