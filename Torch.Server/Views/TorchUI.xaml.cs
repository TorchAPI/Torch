using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using Sandbox;
using Torch.API;
using Torch.Server.Managers;
using MessageBoxResult = System.Windows.MessageBoxResult;
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
            //TODO: data binding for whole server
            DataContext = server;
            InitializeComponent();

            AttachConsole();

            Left = _config.WindowPosition.X;
            Top = _config.WindowPosition.Y;
            Width = _config.WindowSize.X;
            Height = _config.WindowSize.Y;

            Chat.BindServer(server);
            PlayerList.BindServer(server);
            Plugins.BindServer(server);
            LoadConfig((TorchConfig)server.Config);
        }

        private void AttachConsole()
        {
            var doc = LogManager.Configuration.FindTargetByName<FlowDocumentTarget>("wpf")?.Document;
            ConsoleText.Document = doc ?? new FlowDocument(new Paragraph(new Run("No target!")));
            ConsoleText.TextChanged += (sender, args) => ConsoleText.ScrollToEnd();
        }

        public void LoadConfig(TorchConfig config)
        {
            if (!Directory.Exists(config.InstancePath))
                return;

            _config = config;
            Dispatcher.Invoke(() =>
            {
                //InstancePathBox.Text = config.InstancePath;
            });
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => _server.Start());
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to stop the server?", "Stop Server", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
                _server.Invoke(() => _server.Stop());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var newSize = new Point((int)Width, (int)Height);
            _config.WindowSize = newSize;
            var newPos = new Point((int)Left, (int)Top);
            _config.WindowPosition = newPos;

            if (_server?.State == ServerState.Running)
                _server.Stop();

            Environment.Exit(0);
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            //MySandboxGame.Static.Invoke(MySandboxGame.ReloadDedicatedServerSession); use i
        }

        private void InstancePathBox_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var name = ((TextBox)sender).Text;

            if (!Directory.Exists(name))
                return;

            _config.InstancePath = name;
            _server.GetManager<InstanceManager>().LoadInstance(_config.InstancePath);
        }
    }
}
