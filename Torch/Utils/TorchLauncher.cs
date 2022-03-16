using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Torch.Utils
{
    public class TorchLauncher
    {
        private static readonly Dictionary<string, string> Assemblies = new Dictionary<string, string>();

        public static void Launch(params string[] binaryPaths)
        {
            foreach (var file in binaryPaths.SelectMany(path => Directory.EnumerateFiles(path, "*.dll")))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(file);
                    var key = name.Name ?? name.FullName.Split(',')[0];
#if NETFRAMEWORK
                    if (!Assemblies.ContainsKey(key))
                        Assemblies.Add(key, file);
#else
                    Assemblies.TryAdd(key, file);
#endif
                }
                catch (BadImageFormatException)
                {
                    // if we are trying to load native image
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name;
            return Assemblies.TryGetValue(name.IndexOf(',') > 0 ? name.Substring(0, name.IndexOf(',')) : name, out var path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
