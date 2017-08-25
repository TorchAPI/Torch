using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API.Managers;

namespace Torch.API.Session
{
    /// <summary>
    /// Creates a manager for the given session if applicable.
    /// </summary>
    /// <remarks>
    /// This is for creating managers that will live inside the session, not the manager that controls sesssions.
    /// </remarks>
    /// <param name="session">The session to construct a bound manager for</param>
    /// <returns>The manager that will live in the session, or null if none.</returns>
    public delegate IManager SessionManagerFactoryDel(ITorchSession session);

    /// <summary>
    /// Fired when the given session has been completely loaded or is unloading.
    /// </summary>
    /// <param name="session">The session</param>
    public delegate void TorchSessionLoadDel(ITorchSession session);

    /// <summary>
    /// Manages the creation and destruction of <see cref="ITorchSession"/> instances for each <see cref="Sandbox.Game.World.MySession"/> created by Space Engineers.
    /// </summary>
    public interface ITorchSessionManager : IManager
    {
        /// <summary>
        /// Fired when a <see cref="ITorchSession"/> has finished loading.
        /// </summary>
        event TorchSessionLoadDel SessionLoaded;

        /// <summary>
        /// Fired when a <see cref="ITorchSession"/> has begun unloading.
        /// </summary>
        event TorchSessionLoadDel SessionUnloading;

        /// <summary>
        /// The currently running session
        /// </summary>
        ITorchSession CurrentSession { get; }

        /// <summary>
        /// Adds the given factory as a supplier for session based managers
        /// </summary>
        /// <param name="factory">Session based manager supplier</param>
        /// <returns>true if added, false if already present</returns>
        /// <exception cref="ArgumentNullException">If the factory is null</exception>
        bool AddFactory(SessionManagerFactoryDel factory);

        /// <summary>
        /// Remove the given factory from the suppliers for session based managers
        /// </summary>
        /// <param name="factory">Session based manager supplier</param>
        /// <returns>true if removed, false if not present</returns>
        /// <exception cref="ArgumentNullException">If the factory is null</exception>
        bool RemoveFactory(SessionManagerFactoryDel factory);
    }
}
