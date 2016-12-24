using System;
using System.Windows;

namespace Torch.Client
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var client = new TorchClient();

            try
            {
                client.Init();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Torch encountered an error trying to initialize the game.\n{e.Message}");
                return;
            }

            client.Start();
        }
    }
}