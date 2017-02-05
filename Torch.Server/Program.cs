using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using VRage.Game.ModAPI;

namespace Torch.Server
{
    public static class Program
    {
        private static ITorchServer _server;
        private static Logger _log = LogManager.GetLogger("Torch");

        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                {
                    ServiceBase.Run(service);
                }
                return;
            }

            string configName = args.Length > 0 ? args[0] : "TorchConfig.xml";
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), configName);
            var options = new ServerConfig("Torch");
            if (File.Exists(configName))
            {
                _log.Info($"Loading config {configPath}");
                options = ServerConfig.LoadFrom(configPath);
            }
            else
            {
                _log.Info($"Generating default config at {configPath}");
                options.SaveTo(configPath);
            }

            /*
            if (!parser.ParseArguments(args, options))
            {
                _log.Error($"Parsing arguments failed: {string.Join(" ", args)}");
                return;
            }

            if (!string.IsNullOrEmpty(options.Config) && File.Exists(options.Config))
            {
                options = ServerConfig.LoadFrom(options.Config);
                parser.ParseArguments(args, options);
            }*/

            //RestartOnCrash autostart autosave=15 
            //gamepath ="C:\Program Files\Space Engineers DS" instance="Hydro Survival" instancepath="C:\ProgramData\SpaceEngineersDedicated\Hydro Survival"

            /*
            if (options.InstallService)
            {
                var serviceName = $"\"Torch - {options.InstanceName}\"";
                // Working on installing the service properly instead of with sc.exe
                _log.Info($"Installing service '{serviceName}");
                var exePath = $"\"{Assembly.GetExecutingAssembly().Location}\"";
                var createInfo = new ServiceCreateInfo
                {
                    Name = options.InstanceName,
                    BinaryPath = exePath,
                };
                _log.Info("Service Installed");

                var runArgs = string.Join(" ", args.Skip(1));
                _log.Info($"Installing Torch as a service with arguments '{runArgs}'");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create Torch binPath=\"{Assembly.GetExecutingAssembly().Location} {runArgs}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo).WaitForExit();
                _log.Info("Torch service installed");
                return;
            }

            if (options.UninstallService)
            {
                _log.Info("Uninstalling Torch service");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "delete Torch",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo).WaitForExit();
                _log.Info("Torch service uninstalled");
                return;
            }*/

            _server = new TorchServer(options);
            _server.Init();
            _server.Start();
        }
    }
}
