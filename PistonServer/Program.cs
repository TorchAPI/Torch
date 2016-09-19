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
        [STAThread]
        public static void Main(string[] args)
        {
            PistonServer.Init();
            PistonServer.Server.RunArgs = new[] { "-console" };

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
            PistonServer.UI.Show();
        }

        public static void FullRestart()
        {
            PistonServer.Server.StopServer();
            Process.Start("PistonServer.exe", "-autostart");
            Environment.Exit(1);
        }
    }
}
