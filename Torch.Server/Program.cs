using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
#if TORCH_SERVICE
using Microsoft.VisualBasic.Devices;
#endif
using NLog;
using NLog.Fluent;
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
            var workingDir = new FileInfo(typeof(Program).Assembly.Location).Directory!.FullName;
            var binDir = Path.Combine(workingDir, "DedicatedServer64");
            Directory.SetCurrentDirectory(workingDir);
            
            if (Directory.Exists(binDir))
                foreach (var file in Directory.GetFiles(binDir, "System.*.dll"))
                {
                    File.Delete(file);
                }

            TorchLauncher.Launch(workingDir, binDir);
            
            // Breaks on Windows Server 2019
#if TORCH_SERVICE
            if (!new ComputerInfo().OSFullName.Contains("Server 2019") && !Environment.UserInteractive)
            {
                using (var service = new TorchService(args))
                    ServiceBase.Run(service);
                return;
            }
#endif

            var initializer = new Initializer(workingDir);
            if (!initializer.Initialize(args))
                return;

            initializer.Run();
        }
    }
}
