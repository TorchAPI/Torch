using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Torch.Client
{
    public class TorchAssemblyResolver : IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();
        private readonly string[] _paths;
        private readonly string _removablePathPrefix;

        public TorchAssemblyResolver(params string[] paths)
        {
            string location = Assembly.GetEntryAssembly().Location;
            location = location != null ? Path.GetDirectoryName(location) + Path.DirectorySeparatorChar : null;
            _removablePathPrefix = location ?? "";
            _paths = paths;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private string SimplifyPath(string path)
        {
            return path.StartsWith(_removablePathPrefix) ? path.Substring(_removablePathPrefix.Length) : path;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            lock (_assemblies)
            {
                if (_assemblies.TryGetValue(assemblyName, out Assembly asm))
                    return asm;
            }
            lock (AppDomain.CurrentDomain)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    if (asm.GetName().Name.Equals(assemblyName))
                    {
                        lock (this)
                        {
                            _assemblies.Add(assemblyName, asm);
                            return asm;
                        }
                    }
            }
            lock (this)
            {
                foreach (string path in _paths)
                {
                    try
                    {
                        string assemblyPath = Path.Combine(path, assemblyName + ".dll");
                        if (!File.Exists(assemblyPath))
                            continue;
                        _log.Debug("Loading {0} from {1}", assemblyName, SimplifyPath(assemblyPath));
                        LogManager.Flush();
                        Assembly asm = Assembly.LoadFrom(assemblyPath);
                        _assemblies.Add(assemblyName, asm);
                        // Recursively load SE dependencies since they don't trigger AssemblyResolve.
                        // This trades some performance on load for actually working code.
                        foreach (AssemblyName dependency in asm.GetReferencedAssemblies())
                            CurrentDomainOnAssemblyResolve(sender, new ResolveEventArgs(dependency.Name, asm));
                        return asm;
                    }
                    catch
                    {
                        // Ignored
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;
            _assemblies.Clear();
        }
    }
}
