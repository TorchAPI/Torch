using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;
using NLog.Targets;
using Sandbox.Engine.Utils;
using Torch.Utils;
using VRage.FileSystem;

namespace Torch.Server
{
    public class Initializer
    {
        internal static Initializer Instance { get; private set; }

        private static readonly Logger Log = LogManager.GetLogger(nameof(Initializer));
        private bool _init;
        private const string STEAMCMD_DIR = "steamcmd";
        private const string STEAMCMD_ZIP = "temp.zip";
        private static readonly string STEAMCMD_EXE = "steamcmd.exe";
        private const string STEAMCMD_ARGS = "+force_install_dir \"{0}\" +login anonymous +app_update 298740 +quit";
        private TorchServer _server;

        internal Persistent<TorchConfig> ConfigPersistent { get; }
        public TorchConfig Config => ConfigPersistent?.Data;
        public TorchServer Server => _server;

        public Initializer(string basePath, Persistent<TorchConfig> torchConfig)
        {
            Instance = this;
            ConfigPersistent = torchConfig;
        }

        public bool Initialize(string[] args)
        {
            if (_init)
                return false;
#if DEBUG
            //enables logging debug messages when built in debug mode. Amazing.
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "main");
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "console");
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "wpf");
            LogManager.ReconfigExistingLoggers();
            Log.Debug("Debug logging enabled.");
#endif

            // This is what happens when Keen is bad and puts extensions into the System namespace.
            if (!Enumerable.Contains(args, "-noupdate"))
                RunSteamCmd();

            if (!string.IsNullOrEmpty(Config.WaitForPID))
            {
                try
                {
                    var pid = int.Parse(Config.WaitForPID);
                    var waitProc = Process.GetProcessById(pid);
                    Log.Info("Continuing in 5 seconds.");
                    Log.Warn($"Waiting for process {pid} to close");
                    while (!waitProc.HasExited)
                    {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
            }

            _init = true;
            return true;
        }

        public void Run(bool isService, string instanceName, string instancePath)
        {
            _server = new TorchServer(Config, instancePath, instanceName);

            if (isService || Config.NoGui)
            {
                _server.Init();
                _server.Start();
            }
            else
            {
#if !DEBUG
                if (!Config.IndependentConsole)
                {
                    Console.SetOut(TextWriter.Null);
                    NativeMethods.FreeConsole();
                }
#endif
                
                var gameThread = new Thread(() =>
                {
                    _server.Init();

                    if (Config.Autostart || Config.TempAutostart)
                    {
                        Config.TempAutostart = false;
                        _server.Start();
                    }
                });
                
                gameThread.Start();
                
                var ui = new TorchUI(_server);
                
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                
                ui.ShowDialog();
            }
        }
        
        public static void RunSteamCmd()
        {
            var log = LogManager.GetLogger("SteamCMD");

            var path = Environment.GetEnvironmentVariable("TORCH_STEAMCMD") ?? Path.GetFullPath(STEAMCMD_DIR);
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var steamCmdExePath = Path.Combine(path, STEAMCMD_EXE);
            if (!File.Exists(steamCmdExePath))
            {
                try
                {
                    log.Info("Downloading SteamCMD.");
                    using (var client = new HttpClient()) 
                    using (var file = File.Create(STEAMCMD_ZIP))
                        client.GetStreamAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip").Result.CopyTo(file);

                    ZipFile.ExtractToDirectory(STEAMCMD_ZIP, path);
                    File.Delete(STEAMCMD_ZIP);
                    log.Info("SteamCMD downloaded successfully!");
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to download SteamCMD, unable to update the DS.");
                    return;
                }
            }

            log.Info("Checking for DS updates.");
            var steamCmdProc = new ProcessStartInfo(steamCmdExePath)
            {
                Arguments = string.Format(STEAMCMD_ARGS, Environment.GetEnvironmentVariable("TORCH_GAME_PATH") ?? "../"),
                WorkingDirectory = path,
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
    }
}
