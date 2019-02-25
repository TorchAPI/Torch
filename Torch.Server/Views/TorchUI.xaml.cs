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
using NLog.Targets.Wrappers;
using Sandbox;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.Managers;
using MessageBoxResult = System.Windows.MessageBoxResult;

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

            Themes.uiSource = this;
            Themes.SetConfig(_config);
            Title = $"{_config.InstanceName} - Torch {server.TorchVersion}, SE {server.GameVersion}";
        }

        private void AttachConsole()
        {
            const string target = "wpf";
            var doc = LogManager.Configuration.FindTargetByName<FlowDocumentTarget>(target)?.Document;
            if (doc == null)
            {
                var wrapped = LogManager.Configuration.FindTargetByName<WrapperTargetBase>(target);
                doc = (wrapped?.WrappedTarget as FlowDocumentTarget)?.Document;
            }
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
            _server.DedicatedInstance.SaveConfig();
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

            _config.Save(); //you idiot

            if (_server?.State == ServerState.Running)
                _server.Stop();

            Process.GetCurrentProcess().Kill();
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
            _server.Managers.GetManager<InstanceManager>().LoadInstance(_config.InstancePath);
        }
    }
}
