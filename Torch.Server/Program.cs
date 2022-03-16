using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using Torch.API;
using Torch.Utils;

namespace Torch.Server
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var context = CreateApplicationContext();

            SetupLogging();

            var oldTorchCfg = Path.Combine(context.TorchDirectory.FullName, "Torch.cfg");
            var torchCfg = Path.Combine(context.InstanceDirectory.FullName, "Torch.cfg");
            
            if (File.Exists(oldTorchCfg))
                File.Move(oldTorchCfg, torchCfg);

            var config = Persistent<TorchConfig>.Load(torchCfg);
            config.Data.InstanceName = context.InstanceName;
            config.Data.InstancePath = context.InstanceDirectory.FullName;
            
            if (!config.Data.Parse(args))
            {
                Console.WriteLine("Invalid arguments");
                Environment.Exit(1);
            }

            var handler = new UnhandledExceptionHandler(config.Data);
            AppDomain.CurrentDomain.UnhandledException += handler.OnUnhandledException;

            var initializer = new Initializer(config);
            if (!initializer.Initialize(args))
                Environment.Exit(1);

#if DEBUG
            TorchLauncher.Launch(context.TorchDirectory.FullName, context.GameBinariesDirectory.FullName);
#else
            TorchLauncher.Launch(context.TorchDirectory.FullName, Path.Combine(context.TorchDirectory.FullName, "torch64"),
                context.GameBinariesDirectory.FullName);
#endif
            
            CopyNative();
            
            initializer.Run();
        }

        private static void CopyNative()
        {
            var log = LogManager.GetLogger("TorchLauncher");
            
            if (ApplicationContext.Current.GameFilesDirectory.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                log.Warn("Torch directory is readonly. You should copy steam_api64.dll, Havok.dll from bin manually");
                return;
            }

            try
            {
                var apiSource = Path.Combine(ApplicationContext.Current.GameBinariesDirectory.FullName, "steam_api64.dll");
                var apiTarget = Path.Combine(ApplicationContext.Current.GameFilesDirectory.FullName, "steam_api64.dll");
                if (!File.Exists(apiTarget))
                {
                    File.Copy(apiSource, apiTarget);
                }
                else if (File.GetLastWriteTime(apiTarget) < ApplicationContext.Current.GameBinariesDirectory.LastWriteTime)
                {
                    File.Delete(apiTarget);
                    File.Copy(apiSource, apiTarget);
                }

                var havokSource = Path.Combine(ApplicationContext.Current.GameBinariesDirectory.FullName, "Havok.dll");
                var havokTarget = Path.Combine(ApplicationContext.Current.GameFilesDirectory.FullName, "Havok.dll");

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

        private static void SetupLogging()
        {
            var oldNlog = Path.Combine(ApplicationContext.Current.TorchDirectory.FullName, "NLog.config");
            var newNlog = Path.Combine(ApplicationContext.Current.InstanceDirectory.FullName, "NLog.config");
            if (File.Exists(oldNlog) && !File.ReadAllText(oldNlog).Contains("FlowDocument"))
                File.Move(oldNlog, newNlog);
            else if (!File.Exists(newNlog))
                using (var f = File.Create(newNlog))
                    typeof(Program).Assembly.GetManifestResourceStream("Torch.Server.NLog.config")!.CopyTo(f);
            
            Target.Register<LogViewerTarget>(nameof(LogViewerTarget));
            TorchLogManager.RegisterTargets(Environment.GetEnvironmentVariable("TORCH_LOG_EXTENSIONS_PATH") ??
                                            Path.Combine(ApplicationContext.Current.InstanceDirectory.FullName, "LoggingExtensions"));
            
            TorchLogManager.SetConfiguration(new XmlLoggingConfiguration(newNlog));
        }

        private static IApplicationContext CreateApplicationContext()
        {
            var isService = Environment.GetEnvironmentVariable("TORCH_SERVICE")
                ?.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ?? false;
            
            var workingDir = AppContext.BaseDirectory;
            var gamePath = Environment.GetEnvironmentVariable("TORCH_GAME_PATH") ?? workingDir;
            var binDir = Path.Combine(gamePath, "DedicatedServer64");
            Directory.SetCurrentDirectory(gamePath);

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
            
            return new ApplicationContext(new(workingDir), new(gamePath), new(binDir), 
                new(instancePath), instanceName, isService);
        }
    }
}
