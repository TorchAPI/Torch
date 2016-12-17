using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Torch.Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Config _config;
        private TorchFileManager _fileManager;

        public MainWindow()
        {
            InitializeComponent();
            _config = Config.Load();
            Title += $" v{_config.Version}";
            _fileManager = new TorchFileManager(_config.RemoteFilePath);

            CheckSpaceDirectory();
            CheckSEBranch();
            UpdatePistonFiles();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _config.Save();
            base.OnClosing(e);
        }

        private bool CheckSpaceDirectory()
        {
            if (Directory.Exists(_config.SpaceDirectory) && Directory.GetFiles(_config.SpaceDirectory).Any(i => i.Contains("SpaceEngineers.exe")))
                return true;

            var dialog = new SpaceDirPrompt();
            dialog.ShowDialog();

            if (dialog.Success)
            {
                _config.SpaceDirectory = dialog.SelectedDir;
                return true;
            }

            return false;
        }

        private void CheckSEBranch()
        {
            try
            {
                var seAsm = Assembly.LoadFrom(Path.Combine(_config.SpaceDirectory, "VRage.Game.dll"));
                MessageBox.Show("found SE assembly");
                var gameType = seAsm.ExportedTypes.First(x => x.Name == "MyFinalBuildConstants");
                MessageBox.Show("found build constant class");
                var stable = (bool)gameType.GetField("IS_STABLE", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                this.Title += $"SE Branch: {(stable ? "Stable" : "Develop")}";
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to check SE branch.\n\n" + e.Message + "\n\n" + e.StackTrace);
            }
        }

        private void UpdatePistonFiles()
        {
            var i = 0;
            var files = _fileManager.GetDirectoryList();
            foreach (var file in files)
            {
                if (_fileManager.UpdateIfNew(file, _config.SpaceDirectory))
                {
                    i++;
                }
            }

            InfoLabel.Content = $"Updated {i} files";
        }

        private void LaunchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSpaceDirectory())
                return;

            Directory.SetCurrentDirectory(_config.SpaceDirectory);
            Process.Start(Path.Combine(_config.SpaceDirectory, "PistonClient.exe"));
            Environment.Exit(0);
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;

            btn.Content = "Up to date!";
            btn.IsEnabled = false;
        }
    }
}
