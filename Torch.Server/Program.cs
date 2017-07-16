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
using Torch.Server.Views;
using VRage.Game.ModAPI;
using System.IO.Compression;
using System.Net;
using Torch.Server.Managers;
using VRage.FileSystem;
using VRageRender;

namespace Torch.Server
{
    internal static class Program
    {
        private static ITorchServer _server;
        private static Logger _log = LogManager.GetLogger("Torch");
        private static bool _restartOnCrash;
        private static TorchConfig _config;
        private static bool _steamCmdDone;

        /// <summary>
        /// This method must *NOT* load any types/assemblies from the vanilla game, otherwise automatic updates will fail.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            //Ensures that all the files are downloaded in the Torch directory.
            Directory.SetCurrentDirectory(new FileInfo(typeof(Program).Assembly.Location).Directory.ToString());

            foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.old"))
                File.Delete(file);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                {
                    ServiceBase.Run(service);
                }
                return;
            }

            var configName = "TorchConfig.xml";
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), configName);
            if (File.Exists(configName))
            {
                _log.Info($"Loading config {configPath}");
                _config = TorchConfig.LoadFrom(configPath);
            }
            else
            {
                _log.Info($"Generating default config at {configPath}");
                _config = new TorchConfig {InstancePath = Path.GetFullPath("Instance")};

                _log.Warn("Would you like to enable automatic updates? (Y/n):");

                var input = Console.ReadLine() ?? "";
                var autoUpdate = string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.InvariantCultureIgnoreCase);
                _config.GetTorchUpdates = _config.GetPluginUpdates = autoUpdate;
                if (autoUpdate)
                {
                    _log.Info("Automatic updates enabled.");
                    RunSteamCmd();
                }

                _config.Save(configPath);
            }

            if (!_config.Parse(args))
                return;

            _log.Debug(_config.ToString());

            if (!string.IsNullOrEmpty(_config.WaitForPID))
            {
                try
                {
                    var pid = int.Parse(_config.WaitForPID);
                    var waitProc = Process.GetProcessById(pid);
                    _log.Warn($"Waiting for process {pid} to exit.");
                    waitProc.WaitForExit();
                    _log.Info("Continuing in 5 seconds.");
                    Thread.Sleep(5000);
                }
                catch
                {
                    // ignored
                }
            }

            _restartOnCrash = _config.RestartOnCrash;

            if (_config.GetTorchUpdates || _config.Update)
            {
                RunSteamCmd();
            }
            RunServer(_config);
        }

        private const string STEAMCMD_DIR = "steamcmd";
        private const string STEAMCMD_ZIP = "temp.zip";
        private static readonly string STEAMCMD_PATH = $"{STEAMCMD_DIR}\\steamcmd.exe";
        private static readonly string RUNSCRIPT_PATH = $"{STEAMCMD_DIR}\\runscript.txt";
        private const string RUNSCRIPT = @"force_install_dir ../
login anonymous
app_update 298740
quit";

        public static void RunSteamCmd()
        {
            if (_steamCmdDone)
                return;

            var log = LogManager.GetLogger("SteamCMD");

            if (!Directory.Exists(STEAMCMD_DIR))
            {
                Directory.CreateDirectory(STEAMCMD_DIR);
            }

            if (!File.Exists(RUNSCRIPT_PATH))
                File.WriteAllText(RUNSCRIPT_PATH, RUNSCRIPT);

            if (!File.Exists(STEAMCMD_PATH))
            {
                try
                {
                    log.Info("Downloading SteamCMD.");
                    using (var client = new WebClient())
                        client.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", STEAMCMD_ZIP);

                    ZipFile.ExtractToDirectory(STEAMCMD_ZIP, STEAMCMD_DIR);
                    File.Delete(STEAMCMD_ZIP);
                    log.Info("SteamCMD downloaded successfully!");
                }
                catch
                {
                    log.Error("Failed to download SteamCMD, unable to update the DS.");
                    return;
                }
            }

            log.Info("Checking for DS updates.");
            var steamCmdProc = new ProcessStartInfo(STEAMCMD_PATH, "+runscript runscript.txt")
            {
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), STEAMCMD_DIR),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.ASCII
            };
            var cmd = Process.Start(steamCmdProc);

            // ReSharper disable once PossibleNullReferenceException
            while (!cmd.HasExited)
            {
                log.Info(cmd.StandardOutput.ReadLine());
                Thread.Sleep(100);
            }

            _steamCmdDone = true;
        }

        public static void RunServer(TorchConfig config)
        {


            /*
            if (!parser.ParseArguments(args, config))
            {
                _log.Error($"Parsing arguments failed: {string.Join(" ", args)}");
                return;
            }

            if (!string.IsNullOrEmpty(config.Config) && File.Exists(config.Config))
            {
                config = ServerConfig.LoadFrom(config.Config);
                parser.ParseArguments(args, config);
            }*/

            //RestartOnCrash autostart autosave=15 
            //gamepath ="C:\Program Files\Space Engineers DS" instance="Hydro Survival" instancepath="C:\ProgramData\SpaceEngineersDedicated\Hydro Survival"

            /*
            if (config.InstallService)
            {
                var serviceName = $"\"Torch - {config.InstanceName}\"";
                // Working on installing the service properly instead of with sc.exe
                _log.Info($"Installing service '{serviceName}");
                var exePath = $"\"{Assembly.GetExecutingAssembly().Location}\"";
                var createInfo = new ServiceCreateInfo
                {
                    Name = config.InstanceName,
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

            if (config.UninstallService)
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

            _server = new TorchServer(config);

            _server.Init();
            if (config.NoGui || config.Autostart)
            {
                new Thread(() => _server.Start()).Start();
            }

            if (!config.NoGui)
            {
                var ui = new TorchUI((TorchServer)_server);
                ui.ShowDialog();
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var basePath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "DedicatedServer64");
                string asmPath = Path.Combine(basePath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(asmPath))
                    return Assembly.LoadFrom(asmPath);
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            _log.Fatal(ex);
            Console.WriteLine("Exiting in 5 seconds.");
            Thread.Sleep(5000);
            if (_restartOnCrash)
            {
                var exe = typeof(Program).Assembly.Location;
                _config.WaitForPID = Process.GetCurrentProcess().Id.ToString();
                Process.Start(exe, _config.ToString());
            }
            //1627 = Function failed during execution.
            Environment.Exit(1627);
        }
    }
}
