using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Torch.Server.ViewModels;

public class LogViewerViewModel : ViewModel
{
    public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
}


public record LogEntry(DateTime Timestamp, string Message, SolidColorBrush Color);