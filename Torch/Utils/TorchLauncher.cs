using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Torch.API;

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
                    Assemblies.TryAdd(name.Name ?? name.FullName.Split(',')[0], file);
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
            return Assemblies.TryGetValue(name[..name.IndexOf(',')], out var path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
