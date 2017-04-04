using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Torch.API;
using Timer = System.Timers.Timer;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for TorchUI.xaml
    /// </summary>
    public partial class TorchUI : Window
    {
        private TorchServer _server;
        private TorchConfig _config;
        private DateTime _startTime;
        private readonly Timer _uiUpdate = new Timer
        {
            Interval = 1000,
            AutoReset = true,
        };

        public TorchUI(TorchServer server)
        {
            _config = new TorchConfig();
            _server = server;
            InitializeComponent();
            _startTime = DateTime.Now;
            _uiUpdate.Elapsed += UiUpdate_Elapsed;

            Chat.BindServer(server);
            PlayerList.BindServer(server);
            Plugins.BindServer(server);
        }

        public void LoadConfig(TorchConfig config)
        {
            if (!Directory.Exists(config.InstancePath))
                return;

            _config = config;
            Dispatcher.Invoke(() =>
            {
                ConfigControl.LoadDedicatedConfig(config);
                InstancePathBox.Text = config.InstancePath;
            });
        }

        private void UiUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateUptime();
        }

        private void UpdateUptime()
        {
            var currentTime = DateTime.Now;
            var uptime = currentTime - _startTime;

            Dispatcher.Invoke(() => LabelUptime.Content = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _startTime = DateTime.Now;
            Chat.IsEnabled = true;
            PlayerList.IsEnabled = true;
            ((Button) sender).IsEnabled = false;
            BtnStop.IsEnabled = true;
            _uiUpdate.Start();
            ConfigControl.SaveConfig();
            new Thread(() => _server.Start(ConfigControl.Config)).Start();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Chat.IsEnabled = false;
            PlayerList.IsEnabled = false;
            ((Button) sender).IsEnabled = false;
            //HACK: Uncomment when restarting is possible.
            //BtnStart.IsEnabled = true;
            _uiUpdate.Stop();
            _server.Stop();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_server?.IsRunning ?? false)
                _server.Stop();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void InstancePathBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var name = (sender as TextBox).Text;

            _server.SetInstance(null, name);
            _config.InstancePath = name;

            LoadConfig(_config);
        }
    }
}
