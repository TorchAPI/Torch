using NLog;
using Sandbox;
using Torch.API;
using Torch.Managers;
using VRage.Dedicated.RemoteAPI;

namespace Torch.Server.Managers
{
    public class RemoteAPIManager : Manager
    {
        /// <inheritdoc />
        public RemoteAPIManager(ITorchBase torchInstance) : base(torchInstance)
        {
            
        }
        
        /// <inheritdoc />
        public override void Attach()
        {
            Torch.GameStateChanged += TorchOnGameStateChanged;
            base.Attach();
        }

        /// <inheritdoc />
        public override void Detach()
        {
            Torch.GameStateChanged -= TorchOnGameStateChanged;
            base.Detach();
        }

        private void TorchOnGameStateChanged(MySandboxGame game, TorchGameState newstate)
        {
            if (newstate == TorchGameState.Loading && MySandboxGame.ConfigDedicated.RemoteApiEnabled && !string.IsNullOrEmpty(MySandboxGame.ConfigDedicated.RemoteSecurityKey))
            {
                var myRemoteServer = new MyRemoteServer(MySandboxGame.ConfigDedicated.RemoteApiPort, MySandboxGame.ConfigDedicated.RemoteSecurityKey);
                LogManager.GetCurrentClassLogger().Info($"Remote API started on port {myRemoteServer.Port}");
            }
        }
    }
}