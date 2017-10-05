using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.API;

namespace Torch.Utils
{
    public class TorchLauncher
    {
        private const string TorchKey = "TorchWrapper";

        public static bool IsTorchWrapped()
        {
            return AppDomain.CurrentDomain.GetData(TorchKey) != null;
        }

        public static void Launch(string entryPoint, string[] args, params string[] binaryPaths)
        {
            if (IsTorchWrapped())
                throw new Exception("Can't wrap torch twice");
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)?.ToLower().Replace('/', '\\');
            if (exePath == null)
                throw new ArgumentException("Unable to determine executing assembly's path");
            var allPaths = new HashSet<string> { exePath };
            foreach (string other in binaryPaths)
                allPaths.Add(other.ToLower().Replace('/', '\\'));

            var path = new StringBuilder(allPaths.First());
            foreach (string other in binaryPaths)
            {
                if (path.Length > other.Length)
                    path.Remove(other.Length, path.Length - other.Length);
                for (var i = 0; i < path.Length; i++)
                    if (path[i] != other[i])
                    {
                        path.Remove(i, path.Length - i);
                        break;
                    }
            }
            AppDomain.CurrentDomain.AppendPrivatePath(String.Join(Path.PathSeparator.ToString(), allPaths));
            AppDomain.CurrentDomain.SetData(TorchKey, true);
            AppDomain.CurrentDomain.ExecuteAssemblyByName(entryPoint, args);
            return;
            // this would be way better but HAVOK IS UNMANAGED :clang:
            // exclude application base from probing
            var setup = new AppDomainSetup
            {
                ApplicationBase = path.ToString(),
                PrivateBinPathProbe = "",
                PrivateBinPath = string.Join(";", allPaths)
            };
            AppDomain domain = AppDomain.CreateDomain($"TorchDomain-{Assembly.GetEntryAssembly().GetName().Name}-{new Random().Next():X}", null, setup);
            domain.SetData(TorchKey, true);
            domain.ExecuteAssemblyByName(entryPoint, args);
            AppDomain.Unload(domain);
        }
    }
}
