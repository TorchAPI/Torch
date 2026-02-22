using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using Torch.API;
using Torch.API.Managers;
using Torch.Patches;
using Torch.Server.Managers;
using Torch.Server.Views;
using MessageBox = System.Windows.MessageBox;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Rectangle = System.Drawing.Rectangle;
using TextBox = System.Windows.Controls.TextBox;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for TorchUI.xaml
    /// </summary>
    public partial class TorchUI : Window
    {
        private TorchServer _server;
        private TorchConfig _config;
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public static TorchUI Instance;

        private bool _autoscrollLog = true;
        private bool _needScroll;
        private System.Windows.Forms.Timer _scrollTimer;
        public TorchUI(TorchServer server)
        {
            _config = (TorchConfig)server.Config;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SetWindow();
            
            _server = server;
            // TODO: data binding for whole server
            DataContext = server;
            InitializeComponent();
            ConfigTab.Content = new ConfigControl(_server);

            var config = LogManager.Configuration;
            var customTarget = new NlogCustomTarget()
            {
                Name = "NlogCustomTarget"
            };
            config.AddTarget(customTarget);
            var rule = new LoggingRule("*", LogLevel.Warn, customTarget); // Adjust the log level as needed
            config.LoggingRules.Add(rule);

            // Apply changes
            LogManager.Configuration = config;

            AttachConsole();

            Chat.BindServer(server);
            PlayerList.BindServer(server);
            Plugins.BindServer(server);
            LoadConfig((TorchConfig)server.Config);

            Themes.uiSource = this;
            Themes.SetConfig(_config);
            Title = $"{_config.InstanceName} - Torch {server.TorchVersion}, SE {server.GameVersion}";
            Instance = this;

            EntityManagerTab.Content = new EntitiesControl(_server);
            
            Loaded += TorchUI_Loaded;
        }

        private void TorchUI_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = FindDescendant<ScrollViewer>(ConsoleText);
            scrollViewer.ScrollChanged += ConsoleText_OnScrollChanged;

            _scrollTimer = new System.Windows.Forms.Timer();
            _scrollTimer.Tick += ScrollIfNeed;
            _scrollTimer.Interval = 120;
            _scrollTimer.Start();

            // Show analytics banner if enabled and not hidden
            if (_config.EnableAnalytics && !_config.AnalyticsBannerHidden)
                AnalyticsBanner.Visibility = Visibility.Visible;

            // Keep banner in sync if config values change at runtime
            _config.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(TorchConfig.EnableAnalytics) ||
                    args.PropertyName == nameof(TorchConfig.AnalyticsBannerHidden))
                    Dispatcher.Invoke(() =>
                        AnalyticsBanner.Visibility = _config.EnableAnalytics && !_config.AnalyticsBannerHidden
                            ? Visibility.Visible
                            : Visibility.Collapsed);
            };

            _server.GameStateChanged += (game, state) =>
            {
                if (state == TorchGameState.Loaded && _config.MinimizeOnServerStart)
                    BtnStart.Dispatcher.Invoke(() =>  // Cheap way to get to UI thread
                    {
                        WindowState = WindowState.Minimized;
                    });
            };
        }

        private void SetWindow()
        {
            // Set the default window size if no position is saved
            if ( _config.WindowWidth == 0 && _config.WindowHeight == 0)
            {
                Width = 980;
                Height = 588;
            }
            else
            {
                Width = _config.WindowWidth;
                Height = _config.WindowHeight;
            }
            
            // Only restore if visible on a screen, otherwise let windows position it.
            const int tolerance = 10;
            var rect = new Rectangle(_config.WindowX, _config.WindowY, _config.WindowWidth, _config.WindowHeight);
            if (Screen.AllScreens.Any(s =>
                {
                    Rectangle area = s.WorkingArea;
                    area.Inflate(tolerance, tolerance);
                    return area.Contains(rect);
                }))
            {
                Left = _config.WindowX;
                Top = _config.WindowY;
            }

            LocationChanged += (_, args) =>
            {
                if (!_config.SaveWindowChanges) return;
                _config.WindowX = (int)Left;
                _config.WindowY = (int)Top;
                _config.Save();
            };
            SizeChanged += (_, args) =>
            {
                if (!_config.SaveWindowChanges) return;
                _config.WindowHeight = (int)args.NewSize.Height;
                _config.WindowWidth = (int)args.NewSize.Width;
            };
            
            if (_config.StartMinimized)
                WindowState = WindowState.Minimized;
        }

        private void ScrollIfNeed(object sender, EventArgs e)
        {
            if (_autoscrollLog && _needScroll)
            {
                ConsoleText.ScrollToEnd();
                _needScroll = false;
            }
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
            ConsoleText.FontSize = _config.FontSize;
            ConsoleText.Document = doc ?? new FlowDocument(new Paragraph(new Run("No target!")));
            ConsoleText.TextChanged += ConsoleText_OnTextChanged;
        }

        public static T FindDescendant<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return default(T);
            int numberChildren = VisualTreeHelper.GetChildrenCount(obj);
            if (numberChildren == 0) return default(T);

            for (int i = 0; i < numberChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T)
                {
                    return (T)child;
                }
            }

            for (int i = 0; i < numberChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                var potentialMatch = FindDescendant<T>(child);
                if (potentialMatch != default(T))
                {
                    return potentialMatch;
                }
            }

            return default(T);
        }

        private void ConsoleText_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            _needScroll = true;
        }
        
        private void ConsoleText_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer) sender;
            if (e.ExtentHeightChange == 0)
            {
                // User change.
                _autoscrollLog = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
            }
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

        private void AnalyticsPrivacyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void AnalyticsHideLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _config.AnalyticsBannerHidden = true;
            _config.Save();
            e.Handled = true;
        }

        private void AnalyticsDisableLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _config.EnableAnalytics = false;
            _log.Info("Analytics disabled from UI banner. Set EnableAnalytics=true in Torch.cfg to re-enable.");
            e.Handled = true;
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

            _log.Info("Closing Torch...");
            if (_server?.State == ServerState.Running)
                _server.Stop();

            _scrollTimer.Stop();
            
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
