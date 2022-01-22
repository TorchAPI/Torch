#if TORCH_SERVICE
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Server
{
    [RunInstaller(true)]
    public class TorchServiceInstaller : Installer
    {
        private ServiceInstaller _serviceInstaller;

        public TorchServiceInstaller()
        {
            _serviceInstaller.DisplayName = "Torch (SEDS)";
            _serviceInstaller.Description = "Service for Torch (SE Dedicated Server)";
        }

        /// <inheritdoc />
        public override void Install(IDictionary stateSaver)
        {
            GetServiceName();
            base.Install(stateSaver);
        }

        /// <inheritdoc />
        public override void Uninstall(IDictionary savedState)
        {
            GetServiceName();
            base.Uninstall(savedState);
        }

        private void GetServiceName()
        {
            var name = Context.Parameters["name"];
            if (string.IsNullOrEmpty(name))
                return;

            _serviceInstaller.DisplayName = name;
            _serviceInstaller.ServiceName = name;
        }
    }
}
#endif