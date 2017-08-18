using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Managers
{
    public interface IDependencyProvider
    {
        /// <summary>
        /// Gets the manager that provides the given type.  If there is no such manager, returns null.
        /// </summary>
        /// <param name="type">Type of manager</param>
        /// <returns>manager, or null if none exists</returns>
        IManager GetManager(Type type);
    }

    public static class DependencyProviderExtensions
    {
        /// <summary>
        /// Gets the manager that provides the given type.  If there is no such manager, returns null.
        /// </summary>
        /// <typeparam name="T">Type of manager</typeparam>
        /// <returns>manager, or null if none exists</returns>
        public static T GetManager<T>(this IDependencyProvider depProvider) where T : class, IManager
        {
            return (T)depProvider.GetManager(typeof(T));
        }
    }

}
