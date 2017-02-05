using System;
using System.Windows;
using NLog;

namespace Torch.Client
{
    public static class Program
    {
        private static Logger _log = LogManager.GetLogger("Torch");

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var client = new TorchClient();

            try
            {
                client.Init();
            }
            catch (Exception e)
            {
                _log.Fatal("Torch encountered an error trying to initialize the game.");
                _log.Fatal(e);
                return;
            }

            client.Start();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.StackTrace, ex.Message);
        }
    }
}