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
using Sandbox;
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

        public TorchUI(TorchServer server)
        {
            _config = (TorchConfig)server.Config;
            _server = server;
            InitializeComponent();

            Left = _config.WindowPosition.X;
            Top = _config.WindowPosition.Y;
            Width = _config.WindowSize.X;
            Height = _config.WindowSize.Y;

            //TODO: data binding for whole server
            DataContext = server;
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

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _config.Save();
            ConfigControl.SaveConfig();
            new Thread(_server.Start).Start();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var newSize = new Point((int)Width, (int)Height);
            _config.WindowSize = newSize;
            var newPos = new Point((int)Left, (int)Top);
            _config.WindowPosition = newPos;
            _config.Save();

            if (_server?.State == ServerState.Running)
                _server.Stop();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            //MySandboxGame.Static.Invoke(MySandboxGame.ReloadDedicatedServerSession); use i
        }

        private void InstancePathBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var name = ((TextBox)sender).Text;

            _config.InstancePath = name;

            LoadConfig(_config);
        }
    }
}
