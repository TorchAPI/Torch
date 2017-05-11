using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Server
{
    public class CommandLine
    {
        public TorchConfig Config { get; }
        private string _argPrefix;

        [Arg("instancepath", "Server data folder where saves and mods are stored")]
        public string InstancePath { get => Config.InstancePath; set => Config.InstancePath = value; }

        public CommandLine(TorchConfig config, string argPrefix)
        {
            Config = config;
            _argPrefix = argPrefix;
        }

        public PropertyInfo[] GetArgs()
        {
            return typeof(CommandLine).GetProperties().Where(p => p.HasAttribute<ArgAttribute>()).ToArray();
        }

        public string GetHelp()
        {
            var sb = new StringBuilder();

            foreach (var property in GetArgs())
            {
                var attr = property.GetCustomAttribute<ArgAttribute>();
                sb.AppendLine($"{_argPrefix}{attr.Name.PadRight(20)}{attr.Description}");
            }

            return sb.ToString();
        }

        public void Run(string[] args)
        {
            if (args[0] == $"{_argPrefix}help")
            {
                Console.WriteLine(GetHelp());
                return;
            }

            var properties = GetArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith(_argPrefix))
                    continue;

                foreach (var property in properties)
                {
                    var argName = property.GetCustomAttribute<ArgAttribute>()?.Name;
                    if (argName == null)
                        continue;

                    try
                    {
                        if (string.Compare(argName, 0, args[i], 1, argName.Length, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            if (property.PropertyType == typeof(bool))
                                property.SetValue(this, true);

                            if (property.PropertyType == typeof(string))
                                property.SetValue(this, args[++i]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error parsing arg {argName}");
                    }
                }
            }
            
        }

        private class ArgAttribute : Attribute
        {
            public string Name { get; }
            public string Description { get; }
            public ArgAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }
    }
}
