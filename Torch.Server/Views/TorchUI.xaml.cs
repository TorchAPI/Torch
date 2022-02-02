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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NLog;
using NLog.Targets.Wrappers;
using Sandbox;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.Managers;
using Torch.Server.ViewModels;
using Torch.Server.Views;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for TorchUI.xaml
    /// </summary>
    public partial class TorchUI : Window
    {
        private readonly TorchServer _server;
        private ITorchConfig Config => _server.Config;

        public TorchUI(TorchServer server)
        {
            _server = server;
            //TODO: data binding for whole server
            DataContext = server;
            
            WindowStartupLocation = WindowStartupLocation.Manual;
            Width = Config.WindowWidth;
            Height = Config.WindowHeight;
            InitializeComponent();
            ConsoleText.FontSize = Config.FontSize;

            Loaded += OnLoaded;

            //Left = _config.WindowPosition.X;
            //Top = _config.WindowPosition.Y;
            //Width = _config.WindowSize.X;
            //Height = _config.WindowSize.Y;

            Chat.BindServer(server);
            PlayerList.BindServer(server);
            Plugins.BindServer(server);
            
            if (Config.EntityManagerEnabled)
            {
                EntityManagerTab.Content = new EntitiesControl();
            }

            Themes.uiSource = this;
            Themes.SetConfig((TorchConfig) Config);
            Title = $"{Config.InstanceName} - Torch {server.TorchVersion}, SE {server.GameVersion}";
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachConsole();
        }

        private void AttachConsole()
        {
            const string targetName = "wpf";
            var target = LogManager.Configuration.FindTargetByName<LogViewerTarget>(targetName);
            if (target == null)
            {
                var wrapped = LogManager.Configuration.FindTargetByName<WrapperTargetBase>(targetName);
                target = wrapped?.WrappedTarget as LogViewerTarget;
            }
            if (target is null) return;
            var viewModel = (LogViewerViewModel)ConsoleText.DataContext;
            target.LogEntries = viewModel.LogEntries;
            target.TargetContext = SynchronizationContext.Current;
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
            // Can't save here or you'll persist all the command line arguments
            //
            //var newSize = new Point((int)Width, (int)Height);
            //_config.WindowSize = newSize;
            //var newPos = new Point((int)Left, (int)Top);
            //_config.WindowPosition = newPos;

            //_config.Save(); //you idiot

            if (_server?.State == ServerState.Running)
                _server.Stop();

            Process.GetCurrentProcess().Kill();
        }
    }
}
