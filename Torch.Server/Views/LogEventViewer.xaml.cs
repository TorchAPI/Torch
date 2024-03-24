using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NLog;
using Torch.Patches;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Torch.Server.Views
{
    
    public class LogViewModel
    {
        public ObservableCollection<LogEvent> LogEvents { get; set; }

        public LogViewModel()
        {
            LogEvents = new ObservableCollection<LogEvent>();
            // Optionally, populate your collection with initial data here
        }
    }

    public class LogEvent
    {
        public string Time { get; set; }
        public string Level { get; set; }
        public string Class { get; set; }
        public string Message { get; set; }
    }
    public partial class LogEventViewer : UserControl
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private ObservableCollection<LogEvent> Events = new ObservableCollection<LogEvent>();

        string[] logLevels =
        {
            "No filter",
            "Warn",
            "Error",
            "Fatal"
        };
            
        string[] classFilters =
        {
            "No filter",
        };
        public LogEventViewer()
        {
            InitializeComponent();
            
            //array of filterable log levels

            //populate the log level filter combo box
            LevelFilterComboBox.ItemsSource = logLevels;
            ClassFilterComboBox.ItemsSource = classFilters;
            
            DataContext = new LogViewModel();

            NlogCustomTarget.LogEventReceived += LogEvent;
            TorchServer.Instance.SessionUnloading += Unloading;


            //on loaded event
            Loaded += (sender, args) =>
            {
                //set the log level filter to the first item in the combo box
                LevelFilterComboBox.SelectedIndex = 0;
                ClassFilterComboBox.SelectedIndex = 0;
            };
        }

        private void Unloading()
        {
            NlogCustomTarget.LogEventReceived -= LogEvent;
        }

        private void LogEvent(LogEventInfo obj)
        {
            // Use Dispatcher.Invoke to ensure that the following code block is executed on the UI thread.
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Check if the class (LoggerName) is already in the list to avoid InvalidOperationException
                    if (!classFilters.Any(c => string.Equals(c, obj.LoggerName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // It's safe to manipulate the UI here since we're on the UI thread.
                        var updatedClassFilters = classFilters.Append(obj.LoggerName).ToArray();
                        ClassFilterComboBox.ItemsSource = updatedClassFilters;
                        classFilters = updatedClassFilters; // Ensure classFilters is updated for future checks.
                    }
            
                    AddRow(obj.Level.ToString(), obj.LoggerName, (obj.Message == "{0}" ? obj.FormattedMessage : obj.Message));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }
        private void LevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClassFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            LevelFilterComboBox.SelectedIndex = 0; // Reset to "No filter"
            ClassFilterComboBox.SelectedIndex = 0; // Reset to "No filter"
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            try
            {
                string selectedLevel = LevelFilterComboBox.SelectedItem as string;
                string selectedClass = ClassFilterComboBox.SelectedItem as string;

                // Use LINQ to apply filters more concisely
                var filtered = ((LogViewModel)DataContext).LogEvents.Where(logEvent =>
                {
                    bool levelMatch = selectedLevel == "No filter" ||
                                      logEvent.Level.Equals(selectedLevel, StringComparison.OrdinalIgnoreCase);
                    bool classMatch = selectedClass == "No filter" ||
                                      logEvent.Class.Equals(selectedClass, StringComparison.OrdinalIgnoreCase);
                    return levelMatch && classMatch;
                }).ToList();

                // Update the DataGrid's ItemsSource with the filtered list
                LogEventViewerDataGrid.ItemsSource = new ObservableCollection<LogEvent>(filtered);

                LogEventViewerDataGrid.Items.Refresh();
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        private void LogEventViewerDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is LogEvent logEvent)
            {
                var messageWindow = new LogMessageWindow
                {
                    Owner = Window.GetWindow(this) // Set owner if you want
                };
                messageWindow.MessageTextBox.Text = logEvent.Message;
                messageWindow.ShowDialog(); // Show the window as a dialog
            }
        }
        
        //method to add row to datagrid
        private void AddRow(string level, string caller, string message)
        {
            var logEvent = new LogEvent()
            {
                Time = DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss"),
                Level = level,
                Class = caller,
                Message = message
            };
            
            Events.Add(logEvent);
            
            //add to datagrid in reverse order
            ((LogViewModel) DataContext).LogEvents.Insert(0, logEvent);
            ApplyFilters();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ((LogViewModel) DataContext).LogEvents.Clear();
        }
    }
}
