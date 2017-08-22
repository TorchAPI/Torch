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
using Torch.Session;

namespace Torch.Session
{
    /// <summary>
    /// Manages the creation and destruction of <see cref="TorchSession"/> instances for each <see cref="MySession"/> created by Space Engineers.
    /// </summary>
    public class TorchSessionManager : Manager, ITorchSessionManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private TorchSession _currentSession;

        /// <inheritdoc/>
        public ITorchSession CurrentSession => _currentSession;

        private readonly HashSet<SessionManagerFactory> _factories = new HashSet<SessionManagerFactory>();

        public TorchSessionManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        /// <inheritdoc/>
        public bool AddFactory(SessionManagerFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory must be non-null");
            return _factories.Add(factory);
        }

        /// <inheritdoc/>
        public bool RemoveFactory(SessionManagerFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory must be non-null");
            return _factories.Remove(factory);
        }

        private void SessionLoaded()
        {
            if (_currentSession != null)
            {
                _log.Warn($"Override old torch session {_currentSession.KeenSession.Name}");
                _currentSession.Detach();
            }

            _log.Info($"Starting new torch session for {MySession.Static.Name}");
            _currentSession = new TorchSession(Torch, MySession.Static);
            foreach (SessionManagerFactory factory in _factories)
            {
                IManager manager = factory(CurrentSession);
                if (manager != null)
                    CurrentSession.Managers.AddManager(manager);
            }
            (CurrentSession as TorchSession)?.Attach();
        }

        private void SessionUnloaded()
        {
            if (_currentSession == null)
                return;
            _log.Info($"Unloading torch session for {_currentSession.KeenSession.Name}");
            _currentSession.Detach();
            _currentSession = null;
        }

        /// <inheritdoc/>
        public override void Attach()
        {
            MySession.AfterLoading += SessionLoaded;
            MySession.OnUnloaded += SessionUnloaded;
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            _currentSession?.Detach();
            _currentSession = null;
            MySession.AfterLoading -= SessionLoaded;
            MySession.OnUnloaded -= SessionUnloaded;
        }
    }
}
