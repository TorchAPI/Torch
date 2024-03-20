using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
        public DateTime Time { get; set; }
        public string Level { get; set; }
        public string Class { get; set; }
        public string Message { get; set; }
    }
    public partial class LogEventViewer : UserControl
    {
        public LogEventViewer()
        {
            InitializeComponent();
            
            //array of filterable log levels
            var logLevels = new[]
            {
                "No filter",
                "Warn",
                "Error",
                "Fatal"
            };
            
            var classFilters = new[]
            {
                "No filter",
            };
            
            //populate the log level filter combo box
            LevelFilterComboBox.ItemsSource = logLevels;
            ClassFilterComboBox.ItemsSource = classFilters;
            
            DataContext = new LogViewModel();
            
            //on loaded event
            Loaded += (sender, args) =>
            {
                //set the log level filter to the first item in the combo box
                LevelFilterComboBox.SelectedIndex = 0;
                ClassFilterComboBox.SelectedIndex = 0;
            };
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {

        }
        
        //method to add row to datagrid
        public void AddRow(string level, string caller, string message)
        {
            LogEventViewerDataGrid.Items.Add(new LogEvent()
            {
                Time = DateTime.Now,
                Level = level,
                Class = caller,
                Message = message
            });
        }
    }
}
