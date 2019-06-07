using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using NLog;
using NLog.Targets;
using Torch.Utils;

namespace Torch.Server
{
    internal static class Program
    {
        /// <remarks>
        /// This method must *NOT* load any types/assemblies from the vanilla game, otherwise automatic updates will fail.
        /// </remarks>
        [STAThread]
        public static void Main(string[] args)
        {
            
            
            Target.Register<FlowDocumentTarget>("FlowDocument");
            //Ensures that all the files are downloaded in the Torch directory.
            var workingDir = new FileInfo(typeof(Program).Assembly.Location).Directory.ToString();
            var binDir = Path.Combine(workingDir, "DedicatedServer64");
            Directory.SetCurrentDirectory(workingDir);

            //HACK for block skins update
            foreach (var f in Directory.EnumerateFiles(workingDir, "System.*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(f);
            }
            
            if (!TorchLauncher.IsTorchWrapped())
            {
                TorchLauncher.Launch(Assembly.GetEntryAssembly().FullName, args, binDir);
                return;
            }

            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                    ServiceBase.Run(service);
                return;
            }

            var initializer = new Initializer(workingDir);
            if (!initializer.Initialize(args))
                return;

            initializer.Run();
        }
    }
}