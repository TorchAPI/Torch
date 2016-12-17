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

namespace Torch.Server
{
    public static class Program
    {
        private static TorchServer _server = new TorchServer();

        [STAThread]
        public static void Main(string[] args)
        {
            _server.Init();
            _server.Server.RunArgs = new[] { "-console" };

            if (args.Contains("-nogui"))
                _server.Server.StartServer();
            else
                StartUI();

            if (args.Contains("-autostart") && !_server.Server.IsRunning)
                _server.Server.StartServerThread();

            Dispatcher.Run();
        }

        public static void StartUI()
        {
            Thread.CurrentThread.Name = "UI Thread";
            _server.UI.Show();
        }

        public static void FullRestart()
        {
            _server.Server.StopServer();
            Process.Start("TorchServer.exe", "-autostart");
            Environment.Exit(1);
        }
    }
}
