using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers;

namespace Torch.Commands
{
    public class TorchCommands : CommandModule
    {
#if DEBUG
        [Command("fixit")]
        public void Fixit()
        {
            Environment.Exit(0);
        }

        [Command("dbgcmd")]
        public void Dbgcmd()
        {
            var commandManager = ((PluginManager)Context.Torch.Plugins).Commands;
            Console.WriteLine(commandManager.Commands.GetTreeString());
        }
#endif
        [Command("help", "Displays help for a command")]
        public void Help()
        {
            var commandManager = ((PluginManager)Context.Torch.Plugins).Commands;
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Select(x => x.Key);

                var sb = new StringBuilder();

                if (command != null)
                    sb.AppendLine(command.HelpText);

               sb.AppendLine($"Subcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                var topNodeNames = commandManager.Commands.Root.Select(x => x.Key);
                Context.Respond($"Top level commands: {string.Join(", ", topNodeNames)}");
            }
        }

        [Command("ver", "Shows the running Torch version.")]
        public void Version()
        {
            var ver = Context.Torch.TorchVersion;
            Context.Respond($"Torch version: {ver}");
        }

        [Command("plugins", "Lists the currently loaded plugins.")]
        public void Plugins()
        {
            var plugins = Context.Torch.Plugins.Select(p => p.Name);
            Context.Respond($"Loaded plugins: {string.Join(", ", plugins)}");
        }

        [Command("stop", "Stops the server.")]
        public void Stop()
        {
            Context.Respond("Stopping server.");
            Context.Torch.Stop();
        }
    }
}
