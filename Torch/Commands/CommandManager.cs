using System;
using System.Collections.Generic;
using System.Reflection;
using NLog;
using Torch.API;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class PermissionGroup
    {
        public List<ulong> Members { get; }
        public List<Permission> Permissions { get; }
    }

    public class PermissionUser
    {
        public ulong SteamID { get; }
        public List<PermissionGroup> Groups { get; }
        public List<Permission> Permissions { get; }
    }

    public class Permission
    {
        public string[] Path { get; }
        public bool Allow { get; }
    }

    public class CommandManager
    {
        public ITorchBase Torch { get; }
        public char Prefix { get; set; }

        public Dictionary<string, Command> Commands { get; } = new Dictionary<string, Command>();
        private Logger _log = LogManager.GetLogger(nameof(CommandManager));

        public CommandManager(ITorchBase torch, char prefix = '/')
        {
            Torch = torch;
            Prefix = prefix;
        }

        public bool HasPermission(ulong steamId, Command command)
        {
            return true;
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

                foreach (var method in type.GetMethods())
                {
                    var commandAttrib = method.GetCustomAttribute<CommandAttribute>();
                    if (commandAttrib == null)
                        continue;

                    var command = new Command(plugin, method);
                    _log.Info($"Registering command '{string.Join(".", command.Path)}' from plugin '{plugin.Name}'");
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
        }
    }
}
