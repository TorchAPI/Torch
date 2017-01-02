using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Torch;
using Torch.API;

namespace Torch.Server
{
    public static class Program
    {
        private static ITorchServer _server;

        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                {
                    ServiceBase.Run(service);
                }
            }
            else
            {
                _server = new TorchServer();
                _server.Init();
                _server.Start();
            }
        }
    }
}
