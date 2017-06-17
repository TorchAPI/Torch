using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Torch
{
    public class CommandLine
    {
        private readonly string _argPrefix;

        public CommandLine(string argPrefix = "-")
        {
            _argPrefix = argPrefix;
        }

        public PropertyInfo[] GetArgs()
        {
            return GetType().GetProperties().Where(p => p.HasAttribute<ArgAttribute>()).ToArray();
        }

        public string GetHelp()
        {
            var sb = new StringBuilder();

            foreach (var property in GetArgs())
            {
                var attr = property.GetCustomAttribute<ArgAttribute>();
                sb.AppendLine($"{_argPrefix}{attr.Name.PadRight(24)}{attr.Description}");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var args = new List<string>();
            foreach (var prop in GetArgs())
            {
                var attr = prop.GetCustomAttribute<ArgAttribute>();
                if (prop.PropertyType == typeof(bool) && (bool)prop.GetValue(this))
                {
                    args.Add($"{_argPrefix}{attr.Name}");
                }
                else if (prop.PropertyType == typeof(string))
                {
                    var str = (string)prop.GetValue(this);
                    if (string.IsNullOrEmpty(str))
                        continue;
                    args.Add($"{_argPrefix}{attr.Name} \"{str}\"");
                }
            }

            return string.Join(" ", args);
        }

        public bool Parse(string[] args)
        {
            if (args.Length == 0)
                return true;

            if (args[0] == $"{_argPrefix}help")
            {
                Console.WriteLine(GetHelp());
                return false;
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
                    catch
                    {
                        Console.WriteLine($"Error parsing arg {argName}");
                    }
                }
            }

            return true;

        }

        public class ArgAttribute : Attribute
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
