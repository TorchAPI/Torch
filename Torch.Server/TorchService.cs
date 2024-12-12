using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Threading;
using NLog;

namespace Torch.Server
{
    class TorchService : ServiceBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public const string Name = "Torch (SEDS)";
        private Initializer _initializer;
        private string[] _args;

        public TorchService(string[] args)
        {
            _args = args;
            var workingDir = new FileInfo(typeof(TorchService).Assembly.Location).Directory.ToString();
            Directory.SetCurrentDirectory(workingDir);
            _initializer = new Initializer(workingDir);

            ServiceName = Name;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanStop = true;
        }

        /// <inheritdoc />
        protected override void OnStart(string[] _)
        {
            base.OnStart(_args);

            _initializer.Initialize(_args);
            _initializer.Run();
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            var mre = new ManualResetEvent(false);
            Task.Run(() => _initializer.Server.Stop());
            if (!mre.WaitOne(TimeSpan.FromMinutes(1)))
                Process.GetCurrentProcess().Kill();
        }
    }
}
