using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.VisualBasic.Devices;
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
            var badDlls = new[]
            {
                "System.Security.Principal.Windows.dll",
                "VRage.Platform.Windows.dll"
            };

            try
            {
                foreach (var file in badDlls)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
            catch (Exception e)
            {
                var log = LogManager.GetCurrentClassLogger();
                log.Error($"Error updating. Please delete the following files from the Torch root folder manually:\r\n{string.Join("\r\n", badDlls)}");
                log.Error(e);
                return;
            }

            try 
            {
                if (!TorchLauncher.IsTorchWrapped())
                {
                    TorchLauncher.Launch(Assembly.GetEntryAssembly().FullName, args, binDir);
                    return;
                }

                // Breaks on Windows Server 2019
                if ((!new ComputerInfo().OSFullName.Contains("Server 2019") &&
                     !new ComputerInfo().OSFullName.Contains("Server 2022") &&
                     !new ComputerInfo().OSFullName.Contains("Server 2025")) &&
                    !Environment.UserInteractive)
                {
                    using (var service = new TorchService(args))
                        ServiceBase.Run(service);
                    return;
                }

                var initializer = new Initializer(workingDir);
                if (!initializer.Initialize(args))
                    return;

                initializer.Run();
            } catch (Exception runException)
            {
                var log = LogManager.GetCurrentClassLogger();
                log.Fatal(runException.ToString());
                return;
            }
        }
    }
}
