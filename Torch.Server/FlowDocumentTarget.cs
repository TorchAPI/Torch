using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using NLog;
using NLog.Targets;

namespace Torch.Server
{
    /// <summary>
    /// NLog target that writes to a <see cref="FlowDocument"/>.
    /// </summary>
    [Target("flowDocument")]
    public sealed class FlowDocumentTarget : TargetWithLayout
    {
        private FlowDocument _document = new FlowDocument { Background = new SolidColorBrush(Colors.Black) };
        private readonly Paragraph _paragraph = new Paragraph();
        private readonly int _maxLines = 500;

        public FlowDocument Document => _document;

        public FlowDocumentTarget()
        {
            _document.Blocks.Add(_paragraph);
        }

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {
            _document.Dispatcher.BeginInvoke(() =>
            {
                var message = $"{Layout.Render(logEvent)}\n";
                _paragraph.Inlines.Add(new Run(message) {Foreground = LogLevelColors[logEvent.Level]});

                // A massive paragraph slows the UI down
                if (_paragraph.Inlines.Count > _maxLines)
                    _paragraph.Inlines.Remove(_paragraph.Inlines.FirstInline);
            });
        }

        private static readonly Dictionary<LogLevel, SolidColorBrush> LogLevelColors = new Dictionary<LogLevel, SolidColorBrush>
        {
            [LogLevel.Trace] = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            [LogLevel.Debug] = new SolidColorBrush(Color.FromRgb(118, 118, 118)),
            [LogLevel.Info] = new SolidColorBrush(Color.FromRgb(242, 242, 242)),
            [LogLevel.Warn] = new SolidColorBrush(Color.FromRgb(180, 0, 158)),
            [LogLevel.Error] = new SolidColorBrush(Color.FromRgb(249, 241, 165)),
            [LogLevel.Fatal] = new SolidColorBrush(Color.FromRgb(197, 15, 31)),
        };
    }
}
