namespace Torch.API.Managers
{
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