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

namespace Torch.Server
{
    public static class Program
    {
        private static ITorchServer _server;
        private static Logger _log = LogManager.GetLogger("Torch");
        private static bool _restartOnCrash;
        public static bool IsManualInstall;
        private static TorchCli _cli;

        /// <summary>
        /// This method must *NOT* load any types/assemblies from the vanilla game, otherwise automatic updates will fail.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            //Ensures that all the files are downloaded in the Torch directory.
            Directory.SetCurrentDirectory(new FileInfo(typeof(Program).Assembly.Location).Directory.ToString());

            IsManualInstall = Directory.GetCurrentDirectory().Contains("DedicatedServer64");
            if (!IsManualInstall)
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
            TorchConfig options;
            if (File.Exists(configName))
            {
                _log.Info($"Loading config {configPath}");
                options = TorchConfig.LoadFrom(configPath);
            }
            else
            {
                _log.Info($"Generating default config at {configPath}");
                options = new TorchConfig();

                if (!IsManualInstall)
                {
                    new ConfigManager().CreateInstance("Instance");
                    options.InstancePath = Path.GetFullPath("Instance");

                    _log.Warn("Would you like to enable automatic updates? (Y/n):");

                    var input = Console.ReadLine() ?? "";
                    var autoUpdate = !input.Equals("n", StringComparison.InvariantCultureIgnoreCase);
                    options.AutomaticUpdates = autoUpdate;
                    if (autoUpdate)
                    {
                        _log.Info("Automatic updates enabled, updating server.");
                        RunSteamCmd();
                    }
                }

                //var setupDialog = new FirstTimeSetup { DataContext = options };
                //setupDialog.ShowDialog();
                options.Save(configPath);
            }

            _cli = new TorchCli { Config = options };
            if (!_cli.Parse(args))
                return;

            _log.Debug(_cli.ToString());

            if (!string.IsNullOrEmpty(_cli.WaitForPID))
            {
                try
                {
                    var pid = int.Parse(_cli.WaitForPID);
                    var waitProc = Process.GetProcessById(pid);
                    _log.Warn($"Waiting for process {pid} to exit.");
                    waitProc.WaitForExit();
                }
                catch
                {
                    // ignored
                }
            }

            _restartOnCrash = _cli.RestartOnCrash;

            if (options.AutomaticUpdates || _cli.Update)
            {
                if (IsManualInstall)
                    _log.Warn("Detected manual install, won't attempt to update DS");
                else
                {
                    RunSteamCmd();
                }
            }
            RunServer(options, _cli);
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
        }

        public static void RunServer(TorchConfig options, TorchCli cli)
        {


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

            if (!cli.NoGui)
            {
                var ui = new TorchUI((TorchServer)_server);
                ui.LoadConfig(options);
                ui.ShowDialog();
            }

            if (cli.NoGui || cli.Autostart)
            {
                _server.Start();
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
            if (_restartOnCrash)
            {
                /* Throws an exception somehow and I'm too lazy to debug it.
                try
                {
                    if (MySession.Static != null && MySession.Static.AutoSaveInMinutes > 0)
                        MySession.Static.Save();
                }
                catch { }*/

                var exe = typeof(Program).Assembly.Location;
                _cli.WaitForPID = Process.GetCurrentProcess().Id.ToString();
                Process.Start(exe, _cli.ToString());
            }
            Environment.Exit(-1);
        }
    }
}
