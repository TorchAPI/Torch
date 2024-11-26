using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers;

namespace Torch.Server.Managers
{
    internal class ConsoleCommandManager : Manager
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        [Dependency]
        private readonly CommandManager _commandManager;

        public ConsoleCommandManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        public override void Attach()
        {
            if (!Torch.Config.NoGui)
                return;

            Log.Info("Starting console command listener");

            new Thread(CommandListener)
            {
                Name = "Console Command Listener",
                IsBackground = true,
            }.Start();
        }

        private void CommandListener()
        {
            while (Torch.GameState < TorchGameState.Unloading)
            {
                var line = Console.ReadLine();

                if (line == null)
                    break;

                Torch.Invoke(() =>
                {
                    if (!_commandManager.HandleCommandFromServer(line, LogResponse))
                        Log.Error("Invalid input '{0}'", line);
                });
            }
        }

        private void LogResponse(TorchChatMessage message)
        {
            Log.Info(message.Message);
        }
    }
}
