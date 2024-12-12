using System;
using System.Collections.Generic;

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
        /// Sorts the dependency manager, then attaches all its registered managers in <see cref="AttachOrder" />
        /// </summary>
        void Attach();

        /// <summary>
        /// Detaches all registered managers in <see cref="DetachOrder"/>
        /// </summary>
        void Detach();

        /// <summary>
        /// The order that managers should be attached in.  (Dependencies, then dependents)
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to determine load order before this dependency manager is initialized</exception>
        IEnumerable<IManager> AttachOrder { get; }

        /// <summary>
        /// The order that managers should be detached in.  (Dependents, then dependencies)
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to determine unload order before this dependency manager is initialized</exception>
        IEnumerable<IManager> DetachOrder { get; }
    }
}
