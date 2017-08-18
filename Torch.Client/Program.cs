using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using NLog;
using MessageBox = System.Windows.MessageBox;

namespace Torch.Client
{
    public static class Program
    {
        public const string SpaceEngineersBinaries = "Bin64";
        private static string _spaceEngInstallAlias = null;
        public static string SpaceEngineersInstallAlias
        {
            get
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (_spaceEngInstallAlias == null)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _spaceEngInstallAlias = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "SpaceEngineersAlias");
                }
                return _spaceEngInstallAlias;
            }
        }

        private static readonly string[] _steamInstallDirectories = new[] {
            @"C:\Program Files\Steam\", @"C:\Program Files (x86)\Steam\"
        };
        private const string _steamSpaceEngineersDirectory = @"steamapps\common\SpaceEngineers\";
        private const string _spaceEngineersVerifyFile = SpaceEngineersBinaries + @"\SpaceEngineers.exe";

        public const string ConfigName = "Torch.cfg";

        private static Logger _log = LogManager.GetLogger("Torch");

#if DEBUG
        [DllImport("kernel32.dll")]
        private static extern void AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern void FreeConsole();
#endif
        public static void Main(string[] args)
        {
#if DEBUG
            try
            {
                AllocConsole();
#endif
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // Early config: Resolve SE install directory.
                if (!File.Exists(Path.Combine(SpaceEngineersInstallAlias, _spaceEngineersVerifyFile)))
                    SetupSpaceEngInstallAlias();

                using (new TorchAssemblyResolver(Path.Combine(SpaceEngineersInstallAlias, SpaceEngineersBinaries)))
                {
                    RunClient();
                }
#if DEBUG
            }
            finally
            {
                FreeConsole();
            }
#endif
        }

        private static void SetupSpaceEngInstallAlias()
        {
            string spaceEngineersDirectory = null;
            foreach (string steamDir in _steamInstallDirectories)
            {
                spaceEngineersDirectory = Path.Combine(steamDir, _steamSpaceEngineersDirectory);
                if (File.Exists(Path.Combine(spaceEngineersDirectory, _spaceEngineersVerifyFile)))
                {
                    _log.Debug("Found Space Engineers in {0}", spaceEngineersDirectory);
                    break;
                }
                _log.Debug("Couldn't find Space Engineers in {0}", spaceEngineersDirectory);
            }
            if (spaceEngineersDirectory == null)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Please select the SpaceEngineers installation folder"
                };
                do
                {
                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        var ex = new FileNotFoundException("Unable to find the Space Engineers install directory, aborting");
                        _log.Fatal(ex);
                        LogManager.Flush();
                        throw ex;
                    }
                    spaceEngineersDirectory = dialog.SelectedPath;
                    if (File.Exists(Path.Combine(spaceEngineersDirectory, _spaceEngineersVerifyFile)))
                        break;
                    if (MessageBox.Show(
                            $"Unable to find {0} in {1}.  Are you sure it's the Space Engineers install directory?",
                            "Invalid Space Engineers Directory", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        break;
                } while (true);  // Repeat until they confirm.
            }
            if (!JunctionLink(SpaceEngineersInstallAlias, spaceEngineersDirectory))
            {
                var ex = new IOException($"Failed to create junction link {SpaceEngineersInstallAlias} => {spaceEngineersDirectory}. Aborting.");
                _log.Fatal(ex);
                LogManager.Flush();
                throw ex;
            }
            string junctionVerify = Path.Combine(SpaceEngineersInstallAlias, _spaceEngineersVerifyFile);
            if (!File.Exists(junctionVerify))
            {
                var ex = new FileNotFoundException($"Junction link is not working.  File {junctionVerify} does not exist");
                _log.Fatal(ex);
                LogManager.Flush();
                throw ex;
            }
        }

        private static bool JunctionLink(string linkName, string targetDir)
        {
            var junctionLinkProc = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkName}\" \"{targetDir}\"")
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.ASCII
            };
            Process cmd = Process.Start(junctionLinkProc);
            // ReSharper disable once PossibleNullReferenceException
            while (!cmd.HasExited)
            {
                string line = cmd.StandardOutput.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    _log.Info(line);
                Thread.Sleep(100);
            }
            if (cmd.ExitCode != 0)
                _log.Error("Unable to create junction link {0} => {1}", linkName, targetDir);
            return cmd.ExitCode == 0;
        }
        
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            _log.Error(ex);
            LogManager.Flush();
            MessageBox.Show(ex.StackTrace, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunClient()
        {
            var client = new TorchClient();

            try
            {
                client.Init();
            }
            catch (Exception e)
            {
                _log.Fatal("Torch encountered an error trying to initialize the game.");
                _log.Fatal(e);
                return;
            }

            client.Start();
        }
    }
}