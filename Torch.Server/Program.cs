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
using System.Security.Policy;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.FileSystem;
using VRageRender;

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
            //Ensures that all the files are downloaded in the Torch directory.
            var workingDir = new FileInfo(typeof(Program).Assembly.Location).Directory.ToString();
            var binDir = Path.Combine(workingDir, "DedicatedServer64");
            Directory.SetCurrentDirectory(workingDir);

            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                using (new TorchAssemblyResolver(binDir))
                {
                    ServiceBase.Run(service);
                }
                return;
            }

            var initializer = new Initializer(workingDir);
            if (!initializer.Initialize(args))
                return;

            initializer.Run();
        }
    }
}
