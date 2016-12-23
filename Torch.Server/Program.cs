using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Torch;
using Torch.API;

namespace Torch.Server
{
    public static class Program
    {
        private static readonly ITorchServer _server = new TorchServer();
        private static TorchUI _ui;

        [STAThread]
        public static void Main(string[] args)
        {
            _server.Init();
            _server.RunArgs = new[] { "-console" };
            _ui = new TorchUI(_server);

            if (args.Contains("-nogui"))
                _server.Start();
            else
                StartUI();

            if (args.Contains("-autostart") && !_server.IsRunning)
                new Thread(() => _server.Start()).Start();

            Dispatcher.Run();
        }

        public static void StartUI()
        {
            Thread.CurrentThread.Name = "UI Thread";
            _ui.Show();
        }

        public static void FullRestart()
        {
            _server.Stop();
            Process.Start("TorchServer.exe", "-autostart");
            Environment.Exit(1);
        }
    }
}
