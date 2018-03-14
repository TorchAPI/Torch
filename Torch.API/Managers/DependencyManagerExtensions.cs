using System;

namespace Torch.API.Managers
{
    public static class DependencyManagerExtensions
    {
        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <param name="managerType">The dependency type to remove</param>
        /// <returns>The manager that was removed, or null if one wasn't removed</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public static IManager RemoveManager(this IDependencyManager depManager, Type managerType)
        {
            IManager mgr = depManager.GetManager(managerType);
            return depManager.RemoveManager(mgr) ? mgr : null;
        }
        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <typeparam name="T">The dependency type to remove</typeparam>
        /// <returns>The manager that was removed, or null if one wasn't removed</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public static IManager RemoveManager<T>(this IDependencyManager depManager)
        {
            return depManager.RemoveManager(typeof(T));
        }

    }
}