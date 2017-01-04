using System;
using System.Collections.Generic;
using System.Reflection;
using Torch.API;

namespace Torch.Commands
{
    public class CommandSystem
    {
        public ITorchBase Server { get; }
        public char Prefix { get; set; }

        public Dictionary<string, Command> Commands { get; } = new Dictionary<string, Command>();

        public CommandSystem(ITorchBase server, char prefix = '/')
        {
            Server = server;
            Prefix = prefix;
        }

        public bool IsCommand(string command)
        {
            return command.Length > 1 && command[0] == Prefix;
        }

        public void RegisterPluginCommands(ITorchPlugin plugin)
        {
            var assembly = plugin.GetType().Assembly;
            foreach (var type in assembly.ExportedTypes)
            {
                if (!type.IsSubclassOf(typeof(CommandModule)))
                    continue;

                var module = (CommandModule)Activator.CreateInstance(type);
                module.Server = Server;
                module.Plugin = plugin;
                foreach (var method in type.GetMethods())
                {
                    var commandAttrib = method.GetCustomAttribute<CommandAttribute>();
                    if (commandAttrib == null)
                        continue;

                    if (Commands.ContainsKey(commandAttrib.Name))
                    {
                        Console.WriteLine($"[ERROR]: Command \"{method.Name}\" is already registered!");
                        continue;
                    }

                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandContext))
                    {
                        //TODO: log invalid command
                        Console.WriteLine($"[ERROR]: Command \"{method.Name}\" has the wrong signature! Must take one CommandContext parameter.");
                        continue;
                    }

                    var command = new Command
                    {
                        Module = module,
                        Name = commandAttrib.Name,
                        Invoke = c => method.Invoke(module, new object[] {c})
                    };

                    Commands.Add(command.Name, command);
                }
            }
        }

        public void HandleCommand(string command, ulong steamId = 0)
        {
            if (!IsCommand(command))
                return;

            var cmdNameEnd = command.Length - command.IndexOf(" ", StringComparison.InvariantCultureIgnoreCase);
            var cmdName = command.Substring(1, cmdNameEnd);
            if (!Commands.ContainsKey(cmdName))
                return;

            string arg = "";
            if (command.Length > cmdNameEnd + 1)
                arg = command.Substring(cmdNameEnd + 1);

            var context = new CommandContext
            {
                //Args = arg,
                SteamId = steamId
            };

            
            Commands[cmdName].Invoke(context);
        }
    }
}
