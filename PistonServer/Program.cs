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
using Piston;

namespace Piston.Server
{
    public static class Program
    {
        public static MainWindow UserInterface = new MainWindow();
        public static Dispatcher MainDispatcher { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Logger.Write("Initializing");
            PistonServer.Server.RunArgs = new[] { "-console" };
            MainDispatcher = Dispatcher.CurrentDispatcher;

            if (args.Contains("-nogui"))

                PistonServer.Server.StartServer();
            else
                StartUI();

            if (args.Contains("-autostart") && !PistonServer.Server.Running)
                PistonServer.Server.StartServerThread();

            Dispatcher.Run();
        }

        public static void StartUI()
        {
            Thread.CurrentThread.Name = "UI Thread";
            UserInterface.Show();
        }

        public static void FullRestart()
        {
            PistonServer.Server.StopServer();
            Process.Start("PistonServer.exe", "-autostart");
            Environment.Exit(1);
        }
    }
}
