using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
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
            //Ensures that all the files are downloaded in the Torch directory.
            var workingDir = new FileInfo(typeof(Program).Assembly.Location).Directory.ToString();
            var binDir = Path.Combine(workingDir, "DedicatedServer64");
            Directory.SetCurrentDirectory(workingDir);

            if (!TorchLauncher.IsTorchWrapped())
            {
                TorchLauncher.Launch(Assembly.GetEntryAssembly().FullName,args,  binDir);
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
