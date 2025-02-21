using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Torch.Server
{
    [RunInstaller(true)]
    public class TorchServiceInstaller : Installer
    {
        private ServiceInstaller _serviceInstaller;

        public TorchServiceInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller();
            _serviceInstaller = new ServiceInstaller();

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            _serviceInstaller.DisplayName = "Torch (SEDS)";
            _serviceInstaller.Description = "Service for Torch (SE Dedicated Server)";
            _serviceInstaller.StartType = ServiceStartMode.Manual;

            _serviceInstaller.ServiceName = TorchService.Name;

            Installers.Add(serviceProcessInstaller);
            Installers.Add(_serviceInstaller);
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
