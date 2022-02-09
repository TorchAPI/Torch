using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using Torch.Utils;

namespace Torch.Server
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var isService = Environment.GetEnvironmentVariable("TORCH_SERVICE")
                ?.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ?? false;
            //Ensures that all the files are downloaded in the Torch directory.
            var workingDir = AppContext.BaseDirectory;
            var binDir = Path.Combine(Environment.GetEnvironmentVariable("TORCH_GAME_PATH") ?? workingDir, "DedicatedServer64");
            Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("TORCH_GAME_PATH") ?? workingDir);
            
            if (!isService && Directory.Exists(binDir))
                foreach (var file in Directory.GetFiles(binDir, "System.*.dll"))
                {
                    File.Delete(file);
                }

            // Breaks on Windows Server 2019
#if TORCH_SERVICE
            if (!new ComputerInfo().OSFullName.Contains("Server 2019") && !Environment.UserInteractive)
            {
                using (var service = new TorchService(args))
                    ServiceBase.Run(service);
                return;
            }
#endif

            var instanceName = Environment.GetEnvironmentVariable("TORCH_INSTANCE") ?? "Instance";
            string instancePath;
            
            if (Path.IsPathRooted(instanceName))
            {
                instancePath = instanceName;
                instanceName = Path.GetDirectoryName(instanceName);
            }
            else
            {
                instancePath = Directory.CreateDirectory(instanceName).FullName;
            }
            
            var oldNlog = Path.Combine(workingDir, "NLog.config");
            var newNlog = Path.Combine(instancePath, "NLog.config");
            if (File.Exists(oldNlog))
                File.Move(oldNlog, newNlog, true);
            else if (!File.Exists(newNlog))
                using (var f = File.Create(newNlog))
                    typeof(Program).Assembly.GetManifestResourceStream("Torch.Server.NLog.config")!.CopyTo(f);

            var oldTorchCfg = Path.Combine(workingDir, "Torch.cfg");
            var torchCfg = Path.Combine(instancePath, "Torch.cfg");
            
            if (File.Exists(oldTorchCfg))
                File.Move(oldTorchCfg, torchCfg, true);

            var config = Persistent<TorchConfig>.Load(torchCfg);
            config.Data.InstanceName = instanceName;
            config.Data.InstancePath = instancePath;
            if (!config.Data.Parse(args))
            {
                Console.WriteLine("Invalid arguments");
                Environment.Exit(1);
            }

            var handler = new UnhandledExceptionHandler(config.Data, isService);
            AppDomain.CurrentDomain.UnhandledException += handler.OnUnhandledException;
            
            Target.Register<LogViewerTarget>(nameof(LogViewerTarget));
            TorchLogManager.RegisterTargets(Environment.GetEnvironmentVariable("TORCH_LOG_EXTENSIONS_PATH") ??
                                            Path.Combine(instancePath, "LoggingExtensions"));
            
            TorchLogManager.SetConfiguration(new XmlLoggingConfiguration(newNlog));

            var initializer = new Initializer(workingDir, config);
            if (!initializer.Initialize(args))
                Environment.Exit(1);

            TorchLauncher.Launch(workingDir, binDir);
            
            initializer.Run(isService, instanceName, instancePath);
        }

        private static void CopyNative(string binPath)
        {
            var log = LogManager.GetLogger("TorchLauncher");
            
            var workingDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            if (workingDir.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                log.Warn("Game directory is readonly. You should copy steam_api64.dll, Havok.dll from bin manually");
                return;
            }

            try
            {
                var apiSource = Path.Combine(binPath, "steam_api64.dll");
                var apiTarget = Path.Combine(workingDir.FullName, "steam_api64.dll");
                if (!File.Exists(apiTarget))
                {
                    File.Copy(apiSource, apiTarget);
                }
                else if (File.GetLastWriteTime(apiTarget) < File.GetLastWriteTime(binPath))
                {
                    File.Delete(apiTarget);
                    File.Copy(apiSource, apiTarget);
                }

                var havokSource = Path.Combine(binPath, "Havok.dll");
                var havokTarget = Path.Combine(workingDir.FullName, "Havok.dll");

                if (!File.Exists(havokTarget))
                {
                    File.Copy(havokSource, havokTarget);
                }
                else if (File.GetLastWriteTime(havokTarget) < File.GetLastWriteTime(havokSource))
                {
                    File.Delete(havokTarget);
                    File.Copy(havokSource, havokTarget);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // file is being used by another process, probably previous torch has not been closed yet
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
    }
}
