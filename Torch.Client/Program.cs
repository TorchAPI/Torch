using System;
using System.Windows;

namespace Torch.Client
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var game = new GameInitializer(args);

            try
            {
                game.TryInit();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Torch encountered an error trying to initialize the game.\n{e.Message}");
                return;
            }

            game.RunGame();
        }
    }
}