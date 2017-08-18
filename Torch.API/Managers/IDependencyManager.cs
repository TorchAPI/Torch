using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Managers
{
    /// <summary>
    /// Manages a set of <see cref="IManager"/> and the dependencies between them.
    /// </summary>
    public interface IDependencyManager : IDependencyProvider
    {
        /// <summary>
        /// Registers the given manager into the dependency system.
        /// </summary>
        /// <remarks>
        /// This method only returns false when there is already a manager registered with a type derived from this given manager,
        /// or when the given manager is derived from an already existing manager.
        /// </remarks>
        /// <param name="manager">Manager to register</param>
        /// <exception cref="InvalidOperationException">When adding a new manager to an initialized dependency manager</exception>
        /// <returns>true if added, false if not</returns>
        bool AddManager(IManager manager);

        /// <summary>
        /// Clears all managers registered with this dependency manager
        /// </summary>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        void ClearManagers();

        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <param name="manager">The manager to remove</param>
        /// <returns>true if successful, false if the manager wasn't found</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        bool RemoveManager(IManager manager);

        /// <summary>
        /// Initializes the dependency manager, and all its registered managers.
        /// </summary>
        void Init();

        /// <summary>
        /// Disposes the dependency manager, and all its registered managers.
        /// </summary>
        void Dispose();

        /// <summary>
        /// The order that managers should be loaded in.  (Dependencies, then dependents)
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to determine load order before this dependency manager is initialized</exception>
        IEnumerable<IManager> LoadOrder { get; }

        /// <summary>
        /// The order that managers should be unloaded in.  (Dependents, then dependencies)
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to determine unload order before this dependency manager is initialized</exception>
        IEnumerable<IManager> UnloadOrder { get; }
    }
}
