using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Piston.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime startTime;
        private Timer uiUpdate = new Timer
        {
            Interval = 1000,
            AutoReset = true,
        };

        public MainWindow()
        {
            InitializeComponent();
            startTime = DateTime.Now;
            uiUpdate.Elapsed += UiUpdate_Elapsed;

            TabControl.Items.Add(new TabItem());
        }

        private void UiUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateUptime();
        }

        private void UpdateUptime()
        {
            var currentTime = DateTime.Now;
            var uptime = currentTime - startTime;

            Dispatcher.Invoke(() => LabelUptime.Content = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            startTime = DateTime.Now;
            Chat.IsEnabled = true;
            PlayerList.IsEnabled = true;
            ((Button) sender).IsEnabled = false;
            BtnStop.IsEnabled = true;
            uiUpdate.Start();
            PistonServer.Server.StartServerThread();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Chat.IsEnabled = false;
            PlayerList.IsEnabled = false;
            ((Button) sender).IsEnabled = false;
            //HACK: Uncomment when restarting is possible.
            //BtnStart.IsEnabled = true;
            uiUpdate.Stop();
            PistonServer.Server.StopServer();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            PistonServer.Server.StopServer();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            Program.FullRestart();
        }
    }
}
