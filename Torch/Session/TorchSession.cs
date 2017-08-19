using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;

namespace Torch.Session
{
    public class TorchSession : ITorchSession
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The Torch instance this session is bound to
        /// </summary>
        public ITorchBase Torch { get; }

        /// <summary>
        /// The Space Engineers game session this session is bound to.
        /// </summary>
        public MySession KeenSession { get; }

        /// <inheritdoc cref="IDependencyManager"/>
        public IDependencyManager Managers { get; }

        public TorchSession(ITorchBase torch, MySession keenSession)
        {
            Torch = torch;
            KeenSession = keenSession;
            Managers = new DependencyManager(torch.Managers);
        }

        internal void Attach()
        {
            Managers.Attach();
        }

        internal void Detach()
        {
            Managers.Detach();
        }
    }
}
