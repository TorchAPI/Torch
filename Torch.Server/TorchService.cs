using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using NLog;
using Torch.API;

namespace Torch.Server
{
    class TorchService : ServiceBase
    {
        public const string Name = "Torch (SEDS)";
        private TorchServer _server;
        private static Logger _log = LogManager.GetLogger("Torch");

        public TorchService()
        {
            ServiceName = Name;
            EventLog.Log = "Application";

            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanStop = true;
        }

        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            _server = new TorchServer("Torch");
            _server.Init();
            _server.RunArgs = args;
            Task.Run(() => _server.Start());
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            _server.Stop();
            base.OnStop();
        }

        /// <inheritdoc />
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        /// <inheritdoc />
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }
    }
}
