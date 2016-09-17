using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Piston;

namespace PistonServer
{
    public static class Program
    {
        public static MainWindow UserInterface = new MainWindow();
        public static Dispatcher MainDispatcher { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            ServerManager.Static.RunArgs = new[] { "-console" };
            MainDispatcher = Dispatcher.CurrentDispatcher;
            Console.WriteLine(MainDispatcher.Thread.ManagedThreadId);
#if DEBUG
            Directory.SetCurrentDirectory(@"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\DedicatedServer64");
#endif

            if (args.Contains("-nogui"))
                ServerManager.Static.StartServer();
            else
                StartUI();

            Dispatcher.Run();
        }

        public static void StartUI()
        {
            Thread.CurrentThread.Name = "UI Thread";
            UserInterface.Show();
        }
    }
}
