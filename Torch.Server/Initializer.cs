using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using NLog;
using Sandbox;
using VRage;

namespace Torch.Server
{
    public class Initializer
    {
        [Obsolete("It's hack. Do not use it!")]
        internal static Initializer Instance { get; private set; }

        private static readonly Logger Log = LogManager.GetLogger(nameof(Initializer));
        private bool _init;
        private const string STEAMCMD_DIR = "steamcmd";
        private const string STEAMCMD_ZIP = "temp.zip";
        private static readonly string STEAMCMD_PATH = $"{STEAMCMD_DIR}\\steamcmd.exe";
        private static readonly string RUNSCRIPT_PATH = $"{STEAMCMD_DIR}\\runscript.txt";

        private const string RUNSCRIPT = @"force_install_dir ../game
login anonymous
app_update 298740 
quit";
        private TorchServer _server;
        private string _basePath;

        private static string GetDedicatedServer64Path(string basePath) => Path.Combine(basePath, "game", "DedicatedServer64");

        private void EnsureDedicatedServer64Symlink()
        {
            var targetPath = GetDedicatedServer64Path(_basePath);
            var linkPath = Path.Combine(_basePath, "DedicatedServer64");

            if (!Directory.Exists(targetPath))
            {
                Log.Warn($"Target DedicatedServer64 folder does not exist at {targetPath}, skipping symlink creation.");
                return;
            }

            if (Directory.Exists(linkPath))
            {
                // Check if it's already a junction pointing to the correct target
                var attr = File.GetAttributes(linkPath);
                if ((attr & FileAttributes.ReparsePoint) != 0)
                {
                    // It's a junction/symlink, we assume it's correct (could verify target but skip for simplicity)
                    Log.Info($"DedicatedServer64 junction already exists at {linkPath}");
                    return;
                }
                else
                {
                    // It's a regular directory - we shouldn't replace it
                    Log.Warn($"DedicatedServer64 already exists as a regular directory at {linkPath}, not creating junction.");
                    return;
                }
            }

            // Create junction using mklink
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        Log.Info($"Created DedicatedServer64 junction at {linkPath} pointing to {targetPath}");
                    }
                    else
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        Log.Warn($"Failed to create junction: {output} {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to create DedicatedServer64 junction: {ex.Message}");
            }
        }

        private void CheckPrerequisites()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Log.Info("Skipping prerequisite checks on non-Windows platform.");
                return;
            }

            bool allOk = true;

            // .NET Framework 4.8
            if (!IsNetFramework48Installed())
            {
                Log.Error(".NET Framework 4.8 is not installed. Please install it from https://go.microsoft.com/fwlink/?LinkId=2085155");
                Log.Error(
                    "Please visit Torch's Wiki - Installation page for more information at https://wiki.torchapi.com/en/installing-torch");
                allOk = false;
            }

            // VC++ 2013 Redistributable (x64)
            if (!IsVcRedist2013Installed())
            {
                Log.Error("Visual C++ 2013 Redistributable (x64) is not installed. Please install it from https://aka.ms/highdpimfc2013x64enu");
                Log.Error("Please visit Torch's Wiki - Installation page for more information at https://wiki.torchapi.com/en/installing-torch");
                allOk = false;
            }

            // VC++ 2019 Redistributable (x64)
            if (!IsVcRedist2019Installed())
            {
                Log.Error("Visual C++ 2019 Redistributable (x64) is not installed. Please install it from https://aka.ms/vc14/vc_redist.x64.exe");
                Log.Error("Please visit Torch's Wiki - Installation page for more information at https://wiki.torchapi.com/en/installing-torch");
                allOk = false;
            }

            if (allOk)
                Log.Info("All prerequisites satisfied.");
            else
            {
                Log.Error("Prerequisites not satisfied. Please install the missing components and try again.");
                Log.Error("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static bool IsNetFramework48Installed()
        {
            try
            {
                using (var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (ndpKey?.GetValue("Release") is int release)
                        return release >= 528040; // .NET 4.8
                }
            }
            catch
            {
                // ignore
            }
            return false;
        }

        private static bool IsVcRedist2013Installed()
        {
            // Check registry keys for x64 and x86
            string[] registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\12.0\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\12.0\VC\Runtimes\x64",
                @"SOFTWARE\Microsoft\VisualStudio\12.0\VC\Runtimes\x86",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\12.0\VC\Runtimes\x86"
            };
            foreach (var path in registryPaths)
            {
                try
                {
                    using (var vcKey = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (vcKey?.GetValue("Installed") is int installed && installed == 1)
                            return true;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Fallback: check for presence of msvcp120.dll in system directory
            string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string dllPath = Path.Combine(systemDir, "msvcp120.dll");
            if (File.Exists(dllPath))
                return true;

            // Also check vcruntime120.dll
            dllPath = Path.Combine(systemDir, "vcruntime120.dll");
            if (File.Exists(dllPath))
                return true;

            return false;
        }

        private static bool IsVcRedist2019Installed()
        {
            // Check registry keys for x64 and x86
            string[] registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86",
                @"SOFTWARE\Microsoft\VisualStudio\14.2\VC\Runtimes\x64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.2\VC\Runtimes\x64"
            };
            foreach (var path in registryPaths)
            {
                try
                {
                    using (var vcKey = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (vcKey?.GetValue("Installed") is int installed && installed == 1)
                            return true;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Fallback: check for presence of vcruntime140.dll in system directory
            string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string dllPath = Path.Combine(systemDir, "vcruntime140.dll");
            if (File.Exists(dllPath))
                return true;

            // Also check msvcp140.dll
            dllPath = Path.Combine(systemDir, "msvcp140.dll");
            if (File.Exists(dllPath))
                return true;

            return false;
        }

        internal Persistent<TorchConfig> ConfigPersistent { get; private set; }
        public TorchConfig Config => ConfigPersistent?.Data;
        public TorchServer Server => _server;

        public Initializer(string basePath)
        {
            _basePath = basePath;
            Instance = this;
        }

        public bool Initialize(string[] args)
        {
            if (_init)
                return false;

            // Adding .net 10 preview stuff might have made optimizations/inlining too fast??
            // the !Debug is called before nlog has loaded so we force it.
            var config = new NLog.Config.XmlLoggingConfiguration("NLog.config", true);
            LogManager.Configuration = config;
            
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += HandleException;
            // LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, "console");  This is a duplicate rule which already exists in Nlog.conf
            LogManager.ReconfigExistingLoggers();
#endif

#if DEBUG
            AppDomain.CurrentDomain.UnhandledException += HandleException;
            //enables logging debug messages when built in debug mode. Amazing.
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "main");
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "console");
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Debug, "wpf");
            LogManager.ReconfigExistingLoggers();
            Log.Debug("Debug logging enabled.");
#endif

            // This is what happens when Keen is bad and puts extensions into the System namespace.
            CheckPrerequisites();

            if (!Enumerable.Contains(args, "-noupdate"))
                RunSteamCmd();

            // Legacy/Plugin Dev Support
            EnsureDedicatedServer64Symlink();

            var basePath = new FileInfo(typeof(Program).Assembly.Location).Directory.ToString();
            var dedicatedServerPath = GetDedicatedServer64Path(basePath);
            var apiSource = Path.Combine(dedicatedServerPath, "steam_api64.dll");
            var apiTarget = Path.Combine(basePath, "steam_api64.dll");

            if (!File.Exists(apiTarget))
            {
                File.Copy(apiSource, apiTarget);
            }
            else if (File.GetLastWriteTime(apiTarget) < File.GetLastWriteTime(apiSource))
            {
                File.Delete(apiTarget);
                File.Copy(apiSource, apiTarget);
            }
            
                   
            // Define the required version once
            var requiredVersion = new Version(9, 86, 62, 31);

            // Steam Client 64-bit DLL
            var clientSource64 = Path.Combine(dedicatedServerPath, "steamclient64.dll");
            var clientTarget64 = Path.Combine(basePath, "steamclient64.dll");
            CopyAndVerifyDll(clientSource64, clientTarget64, requiredVersion);

            // Steam Client 32-bit DLL
            var clientSource = Path.Combine(dedicatedServerPath, "steamclient.dll");
            var clientTarget = Path.Combine(basePath, "steamclient.dll");
            CopyAndVerifyDll(clientSource, clientTarget, requiredVersion);

            // tier0 64-bit DLL
            var tier0Source64 = Path.Combine(dedicatedServerPath, "tier0_s64.dll");
            var tier0Target64 = Path.Combine(basePath, "tier0_s64.dll");
            CopyAndVerifyDll(tier0Source64, tier0Target64, requiredVersion);

            // tier0 32-bit DLL
            var tier0Source = Path.Combine(dedicatedServerPath, "tier0_s.dll");
            var tier0Target = Path.Combine(basePath, "tier0_s.dll");
            CopyAndVerifyDll(tier0Source, tier0Target, requiredVersion);

            // vstdlib 64-bit DLL
            var vstdlibSource64 = Path.Combine(dedicatedServerPath, "vstdlib_s64.dll");
            var vstdlibTarget64 = Path.Combine(basePath, "vstdlib_s64.dll");
            CopyAndVerifyDll(vstdlibSource64, vstdlibTarget64, requiredVersion);

            // vstdlib 32-bit DLL
            var vstdlibSource = Path.Combine(dedicatedServerPath, "vstdlib_s.dll");
            var vstdlibTarget = Path.Combine(basePath, "vstdlib_s.dll");
            CopyAndVerifyDll(vstdlibSource, vstdlibTarget, requiredVersion);

            
            var havokSource = Path.Combine(dedicatedServerPath, "Havok.dll");
            var havokTarget = Path.Combine(basePath, "Havok.dll");

            if (!File.Exists(havokTarget))
            {
                File.Copy(havokSource, havokTarget);   
            }
            else if (File.GetLastWriteTime(havokTarget) < File.GetLastWriteTime(havokSource))
            {   
                File.Delete(havokTarget);
                File.Copy(havokSource, havokTarget);
            }

            InitConfig();
            if (!Config.Parse(args))
                return false;

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
                catch
                {
                    // ignored
                }
            }

            _init = true;
            return true;
        }

        public void Run()
        {
            _server = new TorchServer(Config);

            if (Config.NoGui)
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
                ui.ShowDialog();
            }
        }

        private void InitConfig()
        {
            var configName = "Torch.cfg";
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), configName);
            if (File.Exists(configName))
            {
                Log.Info($"Loading config {configName}");
            }
            else
            {
                Log.Info($"Generating default config at {configPath}");
            }
            ConfigPersistent = Persistent<TorchConfig>.Load(configPath);
        }

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
                catch (Exception e)
                {
                    log.Error("Failed to download SteamCMD, unable to update the DS.");
                    log.Error(e);
                    return;
                }
            }

            log.Info("Checking for DS updates.");
            
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    log.Info($"Retrying SteamCMD update (attempt {attempt})...");
                    log.Info("This may take awhile depending on your internet connection, please be patient.");
                    Thread.Sleep(3000); // brief delay before retry
                }
                
                var steamCmdProc = new ProcessStartInfo(STEAMCMD_PATH, "+runscript runscript.txt")
                {
                    WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), STEAMCMD_DIR),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.ASCII,
                    StandardErrorEncoding = Encoding.ASCII
                };
                var cmd = Process.Start(steamCmdProc);
                if (cmd == null)
                {
                    log.Error("Failed to start SteamCMD process.");
                    continue;
                }

                // Read output and error asynchronously to avoid deadlocks
                var output = new StringBuilder();
                var error = new StringBuilder();
                cmd.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        log.Info(args.Data);
                        output.AppendLine(args.Data);
                    }
                };
                cmd.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        log.Error($"SteamCMD stderr: {args.Data}");
                        error.AppendLine(args.Data);
                    }
                };
                cmd.BeginOutputReadLine();
                cmd.BeginErrorReadLine();
                
                cmd.WaitForExit();
                int exitCode = cmd.ExitCode;
                
                // Ensure all events are processed
                Thread.Sleep(500);
                
                if (exitCode == 0)
                {
                    log.Info("SteamCMD update completed successfully.");
                    return;
                }
                
                log.Warn($"SteamCMD exited with code {exitCode}. Output: {output}");
                if (error.Length > 0)
                    log.Error($"SteamCMD errors: {error}");
                
                // If this was the last attempt, break and let the caller continue (copy will fail later)
                if (attempt == maxAttempts)
                    log.Error("SteamCMD update failed after all attempts. The DS files may be missing.");
            }
        }
        
        private void CopyAndVerifyDll(string sourcePath, string targetPath, Version requiredVersion = null)
        {
            if (!File.Exists(targetPath))
            {
                File.Copy(sourcePath, targetPath);
                return;
            }
        
            if (requiredVersion != null)
            {
                var targetVersion = FileVersionInfo.GetVersionInfo(targetPath);
                var currentVersion = targetVersion.FileVersion;
        
                if (currentVersion != requiredVersion.ToString())
                {
                    File.Delete(targetPath);
                    File.Copy(sourcePath, targetPath);
                }
            }
            else if (File.GetLastWriteTime(targetPath) < File.GetLastWriteTime(sourcePath))
            {
                File.Delete(targetPath);
                File.Copy(sourcePath, targetPath);
            }
        }

        private void LogException(Exception ex)
        {
            if (ex is AggregateException ag)
            {
                foreach (var e in ag.InnerExceptions)
                    LogException(e);

                return;
            }
            
            Log.Fatal(ex);
            
            if (ex is ReflectionTypeLoadException extl)
            {
                foreach (var exl in extl.LoaderExceptions)
                    LogException(exl);

                return;
            }
            
            if (ex.InnerException != null)
            {
                LogException(ex.InnerException);
            }
        }

        private void SendAndDump()
        {
            var shortdate = DateTime.Now.ToString("yyyy-MM-dd");
            var shortdateWithTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            
            var dumpPath = $"Logs\\MiniDumpT{Thread.CurrentThread.ManagedThreadId}-{shortdateWithTime}.dmp";
            Log.Info($"Generating minidump at {dumpPath}");
            var dumpFlags = MyMiniDump.Options.Normal | MyMiniDump.Options.WithProcessThreadData | MyMiniDump.Options.WithThreadInfo;
            MyVRage.Platform.CrashReporting.WriteMiniDump(dumpPath, dumpFlags, IntPtr.Zero);

            if (Config.SendLogsToKeen)
            {
                List<string> additionalFiles = new List<string>();
                if (File.Exists(dumpPath))
                    additionalFiles.Add(dumpPath);
                
                CrashInfo info = MyErrorReporter.BuildCrashInfo();
                MyErrorReporter.ReportNotInteractive($"Logs\\Keen-{shortdate}.log", info.AnalyticId, false,
                    additionalFiles.ToList(), true, string.Empty, string.Empty, info);
            }
            
            if(Config.DeleteMiniDumps)
                File.Delete(dumpPath);
        }

        private void HandleException(object sender, UnhandledExceptionEventArgs e)
        {
            _server.FatalException = true;
            var ex = (Exception)e.ExceptionObject;
            LogException(ex);
            SendAndDump();
            LogManager.Flush();
            if (Config.RestartOnCrash)
            {
                Console.WriteLine("Restarting in 5 seconds.");
                Thread.Sleep(5000);
                var exe = typeof(Program).Assembly.Location;
                Config.WaitForPID = Process.GetCurrentProcess().Id.ToString();
                Process.Start(exe, Config.ToString());
            }
            else
            {
                MessageBox.Show("Torch encountered a fatal error and needs to close. Please check the logs or the Log event viewer for details.", "Torch Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Process.GetCurrentProcess().Kill();
        }
    }
}
