using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Torch.API;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class Command
    {
        public MyPromoteLevel MinimumPromoteLevel { get; }
        public string Name { get; }
        public string Description { get; }
        public string HelpText { get; }
        public Type Module { get; }
        public List<string> Path { get; } = new List<string>();
        public ITorchPlugin Plugin { get; }
        private readonly MethodInfo _method;

        public Command(ITorchPlugin plugin, MethodInfo commandMethod)
        {
            Plugin = plugin;

            var commandAttribute = commandMethod.GetCustomAttribute<CommandAttribute>();
            if (commandAttribute == null)
                throw new TypeLoadException($"Method does not have a {nameof(CommandAttribute)}");

            var permissionAttribute = commandMethod.GetCustomAttribute<PermissionAttribute>();
            MinimumPromoteLevel = permissionAttribute?.PromoteLevel ?? MyPromoteLevel.None;

            if (!commandMethod.DeclaringType.IsSubclassOf(typeof(CommandModule)))
                throw new TypeLoadException($"Command {commandMethod.Name}'s declaring type {commandMethod.DeclaringType.FullName} is not a subclass of {nameof(CommandModule)}");

            var moduleAttribute = commandMethod.DeclaringType.GetCustomAttribute<CategoryAttribute>();

            _method = commandMethod;
            Module = commandMethod.DeclaringType;

            if (moduleAttribute != null)
            {
                Path.AddRange(moduleAttribute.Path);
            }
            Path.AddRange(commandAttribute.Path);

            Name = commandAttribute.Name;
            Description = commandAttribute.Description;
            HelpText = commandAttribute.HelpText;
        }

        public void Invoke(CommandContext context)
        {
            var moduleInstance = (CommandModule)Activator.CreateInstance(Module);
            moduleInstance.Context = context;
            _method.Invoke(moduleInstance, null);
        }
    }
}