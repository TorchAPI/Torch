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
            var pathPrefix = StringUtils.CommonPrefix(allPaths);
#pragma warning disable 618
            AppDomain.CurrentDomain.AppendPrivatePath(string.Join(Path.PathSeparator.ToString(), allPaths));
#pragma warning restore 618
            AppDomain.CurrentDomain.SetData(TorchKey, true);
            AppDomain.CurrentDomain.ExecuteAssemblyByName(entryPoint, args);
        }
    }
}
