using System;
using System.Linq;
using System.Reflection;
using Torch.API;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class Command
    {
        public string Name { get; }
        public string Description { get; }
        public string HelpText { get; }
        public Type Module { get; }
        public string[] Path { get; }
        public ITorchPlugin Plugin { get; }
        private readonly MethodInfo _method;

        public Command(ITorchPlugin plugin, MethodInfo commandMethod)
        {
            Plugin = plugin;

            var commandAttribute = commandMethod.GetCustomAttribute<CommandAttribute>();
            if (commandAttribute == null)
                throw new TypeLoadException($"Method does not have a {nameof(CommandAttribute)}");

            if (!commandMethod.DeclaringType.IsSubclassOf(typeof(CommandModule)))
                throw new TypeLoadException($"Command {commandMethod.Name}'s declaring type {commandMethod.DeclaringType.FullName} is not a subclass of {nameof(CommandModule)}");

            var moduleAttribute = commandMethod.DeclaringType.GetCustomAttribute<CategoryAttribute>();

            _method = commandMethod;
            Module = commandMethod.DeclaringType;
            var path = commandAttribute.Path;
            if (moduleAttribute != null)
            {
                var modPath = moduleAttribute.Path;
                var comPath = commandAttribute.Path;
                path = new string[modPath.Length + comPath.Length];
                modPath.CopyTo(path, 0);
                comPath.CopyTo(path, modPath.Length);
            }
            Path = path;
            Name = commandAttribute.Name;
            Description = commandAttribute.Description;
            HelpText = commandAttribute.HelpText;
        }

        public void Invoke(CommandContext context)
        {
            var moduleInstance = (CommandModule)Activator.CreateInstance(Module);
            moduleInstance.Context = context;
            _method.Invoke(moduleInstance, new object[0]);
        }
    }
}