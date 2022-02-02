using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Torch.Collections;
using Torch.Server.ViewModels;

namespace Torch.Server.Views;

public partial class LogViewerControl : UserControl
{
    private bool _isAutoscrollEnabled = true;
    private readonly List<ScrollViewer> _viewers = new();
    public LogViewerControl()
    {
        InitializeComponent();
        ((LogViewerViewModel)DataContext).LogEntries.CollectionChanged += LogEntriesOnCollectionChanged;
    }

    private void LogEntriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || !_isAutoscrollEnabled)
            return;
        foreach (var scrollViewer in _viewers)
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer) sender;
        if (e.ExtentHeightChange == 0)
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            _isAutoscrollEnabled = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
    }

    private void ScrollViewer_OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewers.Add((ScrollViewer) sender);
    }

    private void ScrollViewer_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewers.Remove((ScrollViewer) sender);
    }
}
