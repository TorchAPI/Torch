﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using NLog;
using NLog.Fluent;
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
                if (IsRunningAsService())
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

        static bool IsRunningAsService()
        {
            // Check if the parent process is services.exe
            using(var currentProcess = Process.GetCurrentProcess())
            using(var parentProcess = Process.GetProcessById(GetParentProcessId(currentProcess.Id)))
                return parentProcess != null && parentProcess.ProcessName.StartsWith("services", StringComparison.OrdinalIgnoreCase);
        }

        static int GetParentProcessId(int processId)
        {
            using(var searcher = new ManagementObjectSearcher($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId={processId}"))
            using(var results = searcher.Get())
                return Convert.ToInt32(results.OfType<ManagementObject>().First()["ParentProcessId"]);
        }
    }
}
