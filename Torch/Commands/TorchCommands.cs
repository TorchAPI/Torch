using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Sandbox.ModAPI;
using Torch;
using Torch.API.Managers;
using Torch.Commands.Permissions;
using Torch.Managers;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class TorchCommands : CommandModule
    {
        [Command("help", "Displays help for a command")]
        [Permission(MyPromoteLevel.None)]
        public void Help()
        {
            var commandManager = Context.Torch.CurrentSession?.Managers.GetManager<CommandManager>();
            if (commandManager == null)
            {
                Context.Respond("Must have an attached session to list commands");
                return;
            }
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Select(x => x.Key);

                var sb = new StringBuilder();

                if (command != null)
                {
                    sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                    sb.Append(command.HelpText);
                }

                if (node.Subcommands.Count() != 0)
                    sb.Append($"\nSubcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                Context.Respond($"Use the {commandManager.Prefix}longhelp command and check your Comms menu for a full list of commands.");
            }
        }

        [Command("longhelp", "Get verbose help. Will send a long message, check the Comms tab.")]
        public void LongHelp()
        {
            var commandManager = Context.Torch.CurrentSession?.Managers.GetManager<CommandManager>();
            if (commandManager == null)
            {
                Context.Respond("Must have an attached session to list commands");
                return;
            }
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Select(x => x.Key);

                var sb = new StringBuilder();

                if (command != null)
                {
                    sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                    sb.Append(command.HelpText);
                }

                if (node.Subcommands.Count() != 0)
                    sb.Append($"\nSubcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                var sb = new StringBuilder("Available commands:\n");
                foreach (var command in commandManager.Commands.WalkTree())
                {
                    if (command.IsCommand)
                        sb.AppendLine($"{command.Command.SyntaxHelp}\n    {command.Command.HelpText}");
                }
                Context.Respond(sb.ToString());
            }
        }

        [Command("ver", "Shows the running Torch version.")]
        [Permission(MyPromoteLevel.None)]
        public void Version()
        {
            var ver = Context.Torch.TorchVersion;
            Context.Respond($"Torch version: {ver}");
        }

        [Command("plugins", "Lists the currently loaded plugins.")]
        [Permission(MyPromoteLevel.None)]
        public void Plugins()
        {
            var plugins = Context.Torch.Managers.GetManager<PluginManager>()?.Plugins.Select(p => p.Value.Name) ?? Enumerable.Empty<string>();
            Context.Respond($"Loaded plugins: {string.Join(", ", plugins)}");
        }

        [Command("stop", "Stops the server.")]
        public void Stop(bool save = true)
        {
            Context.Respond("Stopping server.");
            if (save)
                Context.Torch.Save(Context.Player?.IdentityId ?? 0).Wait();
            Context.Torch.Stop();
        }

        [Command("restart", "Restarts the server.")]
        public void Restart(int countdownSeconds = 10, bool save = true)
        {
            Task.Run(() =>
            {
                var countdown = RestartCountdown(countdownSeconds).GetEnumerator();
                while (countdown.MoveNext())
                {
                    Thread.Sleep(1000);
                }
            });
        }

        private IEnumerable RestartCountdown(int countdown)
        {
            for (var i = countdown; i >= 0; i--)
            {
                if (i >= 60 && i % 60 == 0)
                {
                    Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>().SendMessageAsSelf($"Restarting server in {i / 60} minute{Pluralize(i / 60)}.");
                    yield return null;
                }
                else if (i > 0)
                {
                    if (i < 11)
                        Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>().SendMessageAsSelf($"Restarting server in {i} second{Pluralize(i)}.");
                    yield return null;
                }
                else
                {
                    Context.Torch.Invoke(() =>
                    {
                        Context.Torch.Save(0).Wait();
                        Context.Torch.Restart();
                    });
                    yield break;
                }
            }
        }

        private string Pluralize(int num)
        {
            return num == 1 ? "" : "s";
        }

        /// <summary>
        /// Initializes a save of the game.
        /// Caller id defaults to 0 in the case of triggering the chat command from server.
        /// </summary>
        [Command("save", "Saves the game.")]
        public void Save()
        {
            Context.Respond("Saving game.");
            Context.Torch.Save(Context.Player?.IdentityId ?? 0);
        }
    }
}