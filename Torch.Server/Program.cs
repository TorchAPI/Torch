﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using Microsoft.Xaml.Behaviors.Core;
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
            var binDir = workingDir + @"lib\DedicatedServer64";
            Directory.SetCurrentDirectory(workingDir);

            //HACK for block skins update
            var badDlls = new[]
            {
                "System.Security.Principal.Windows.dll",
                "VRage.Platform.Windows.dll",
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
                //see if arguments contails --wiperoot=false and if not set it to true
                var dontWipeRoot = args.Any(a => a.ToLower().Contains("-wiperoot=false"));

                //TEMPORARY for a few weeks after master deployment to ensure all instances are updated properly
                MigrateFiles(workingDir, dontWipeRoot);
            } catch (Exception e)
            {
                throw new Exception("Error migrating files", e);
            }

            try 
            {
                if (!TorchLauncher.IsTorchWrapped())
                {
                    TorchLauncher.Launch(Assembly.GetEntryAssembly().FullName, args, binDir);
                    return;
                }

                // Breaks on Windows Server 2019
                if (!new ComputerInfo().OSFullName.Contains("Server 2019") && !Environment.UserInteractive)
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

        private static void MigrateFiles(string workingDir, bool dontWipeRoot)
        {
            var torchDir = Path.Combine(workingDir, @"lib\Torch");
            var toMoveToLib = new[]
            {
                "steamcmd",
                "steamapps",
                "TempContent",
                "DedicatedServer64",
                "Content",
                "steamclient.dll",
                "steamclient64.dll",
                "tier0_s.dll",
                "tier0_s64.dll",
                "vstdlib_s.dll",
                "vstdlib_s64.dll",
            };

            var filesToPreserve = new[]
            {
                "Plugins",
                "Instance",
                "Logs",
                "UserData",
                "Torch.Server.exe",
                "Torch.cfg",
                "Torch.Server.pdb",
                "app.config",
                "NLog.config",
                "NLog-user.config",
                "Torch.Server.exe.config",
                "Torch.Server.xml",
                "steam_api64.dll",
                
                //cant be auto deleted
                "Torch.dll",
                "Torch.API.dll",
                "NLog.dll",
            };

            var OldTorchLibs = new[]
            {
                "Torch.dll",
                "Torch.API.dll",
            };
            
            foreach (var file in toMoveToLib)
            {
                if (file.EndsWith(".dll"))
                {
                    //check to see if file exists in current directory
                    var newFile = Path.Combine(workingDir, "lib", file);
                    
                    if (File.Exists(newFile)) continue;
                    if(!Directory.Exists(Path.Combine(workingDir, file))) continue;

                    File.Move(Path.Combine(workingDir, file), newFile);
                }
                else
                {
                    var newDir = Path.Combine(workingDir, "lib", file);
                    
                    if (Directory.Exists(newDir)) continue;
                    if(!Directory.Exists(Path.Combine(workingDir, file))) continue;
                    Directory.Move(Path.Combine(workingDir, file), newDir);
                }
            }

            foreach (var file in Directory.GetFiles(workingDir))
            {
                var fileName = Path.GetFileName(file);

                if (filesToPreserve.Any(x => x == fileName)) continue;

                if (!dontWipeRoot)
                {
                    File.Delete(file);
                }
            }
            
            var oldTorchLibs = new[]
            {
                "Torch.dll",
                "Torch.API.dll"
            };

            //temp until soon after june 1st update
            foreach (var oldLib in oldTorchLibs)
            {
                string source = Path.Combine(workingDir, oldLib);
                if (!File.Exists(source))
                    continue;
                var rand = Path.GetRandomFileName();
                
                var tmpDir = Path.Combine(torchDir, "tmp");
                
                var dest = Path.Combine(tmpDir, rand);
                try {
                    File.Move(source, rand);
                    File.Move(rand, dest);
                } catch (Exception e)
                {
                }
            }
            
        }
    }
}
