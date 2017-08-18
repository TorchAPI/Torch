using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Torch.API.Managers;

namespace Torch.Managers
{
    public sealed class DependencyManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private class DependencyInfo
        {
            internal Type DependencyType => Field.FieldType;
            internal FieldInfo Field { get; private set; }
            internal bool Optional { get; private set; }

            public DependencyInfo(FieldInfo field)
            {
                Field = field;
                Optional = field.GetCustomAttribute<Manager.DependencyAttribute>().Optional;
            }
        }

        /// <summary>
        /// Represents a registered instance of a manager.
        /// </summary>
        private class ManagerInstance
        {
            public IManager Instance { get; private set; }

            internal readonly List<DependencyInfo> Dependencies;
            internal readonly HashSet<Type> SuppliedManagers;
            internal readonly HashSet<ManagerInstance> Dependents;

            public ManagerInstance(IManager manager)
            {
                Instance = manager;

                SuppliedManagers = new HashSet<Type>();
                Dependencies = new List<DependencyInfo>();
                Dependents = new HashSet<ManagerInstance>();
                var openBases = new Queue<Type>();
                openBases.Enqueue(manager.GetType());
                while (openBases.TryDequeue(out Type type))
                {
                    if (!SuppliedManagers.Add(type))
                        continue;

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        if (field.HasAttribute<Manager.DependencyAttribute>())
                            Dependencies.Add(new DependencyInfo(field));

                    foreach (Type parent in type.GetInterfaces())
                        openBases.Enqueue(parent);
                    if (type.BaseType != null)
                        openBases.Enqueue(type.BaseType);
                }
            }

            /// <summary>
            /// Used by <see cref="DependencyManager"/> internally to topologically sort the dependency list.
            /// </summary>
            public int UnsolvedDependencies { get; set; }
        }

        private readonly Dictionary<Type, ManagerInstance> _dependencySatisfaction;
        private readonly List<ManagerInstance> _registeredManagers;
        private readonly List<ManagerInstance> _orderedManagers;
        private readonly DependencyManager _parentManager;

        public DependencyManager(DependencyManager parent = null)
        {
            _dependencySatisfaction = new Dictionary<Type, ManagerInstance>();
            _registeredManagers = new List<ManagerInstance>();
            _orderedManagers = new List<ManagerInstance>();
            _parentManager = parent;
            if (parent == null)
                return;
            foreach (KeyValuePair<Type, ManagerInstance> kv in parent._dependencySatisfaction)
                _dependencySatisfaction.Add(kv.Key, kv.Value);
        }

        private void AddDependencySatisfaction(ManagerInstance instance)
        {
            foreach (Type supplied in instance.SuppliedManagers)
                if (_dependencySatisfaction.ContainsKey(supplied))
                    // When we already have a manager supplying this component we have to unregister it.
                    _dependencySatisfaction[supplied] = null;
                else
                    _dependencySatisfaction.Add(supplied, instance);
        }

        private void RebuildDependencySatisfaction()
        {
            _dependencySatisfaction.Clear();
            foreach (ManagerInstance manager in _registeredManagers)
                AddDependencySatisfaction(manager);
        }

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
        public bool AddManager(IManager manager)
        {
            if (_initialized)
                throw new InvalidOperationException("Can't add new managers to an initialized dependency manager");
            // Protect against adding a manager derived from an existing manager
            if (_registeredManagers.Any(x => x.Instance.GetType().IsInstanceOfType(manager)))
                return false;
            // Protect against adding a manager when an existing manager derives from it.
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (_registeredManagers.Any(x => manager.GetType().IsInstanceOfType(x.Instance)))
                return false;

            var instance = new ManagerInstance(manager);
            _registeredManagers.Add(instance);
            AddDependencySatisfaction(instance);
            return true;
        }

        /// <summary>
        /// Clears all managers registered with this dependency manager
        /// </summary>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public void ClearManagers()
        {
            if (_initialized)
                throw new InvalidOperationException("Can't remove managers from an initialized dependency manager");

            _registeredManagers.Clear();
            _dependencySatisfaction.Clear();
        }

        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <param name="manager">The manager to remove</param>
        /// <returns>true if successful, false if the manager wasn't found</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public bool RemoveManager(IManager manager)
        {
            if (_initialized)
                throw new InvalidOperationException("Can't remove managers from an initialized dependency manager");
            if (manager == null)
                return false;
            for (var i = 0; i < _registeredManagers.Count; i++)
                if (_registeredManagers[i].Instance == manager)
                {
                    _registeredManagers.RemoveAtFast(i);
                    RebuildDependencySatisfaction();
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <param name="type">The dependency type to remove</param>
        /// <returns>The manager that was removed, or null if one wasn't removed</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public IManager RemoveManager(Type type)
        {
            IManager mgr = GetManager(type);
            return RemoveManager(mgr) ? mgr : null;
        }

        /// <summary>
        /// Removes a single manager from this dependency manager.
        /// </summary>
        /// <typeparam name="T">The dependency type to remove</typeparam>
        /// <returns>The manager that was removed, or null if one wasn't removed</returns>
        /// <exception cref="InvalidOperationException">When removing managers from an initialized dependency manager</exception>
        public IManager RemoveManager<T>()
        {
            return RemoveManager(typeof(T));
        }

        private void Sort()
        {
            // Resets the previous sort results
            #region Reset
            _orderedManagers.Clear();
            foreach (ManagerInstance manager in _registeredManagers)
                manager.Dependents.Clear();
            #endregion

            // Creates the dependency graph
            #region Prepare
            var dagQueue = new List<ManagerInstance>();
            foreach (ManagerInstance manager in _registeredManagers)
            {
                var inFactor = 0;
                foreach (DependencyInfo dependency in manager.Dependencies)
                {
                    if (_dependencySatisfaction.TryGetValue(dependency.DependencyType, out var dependencyInstance))
                    {
                        inFactor++;
                        dependencyInstance.Dependents.Add(manager);
                    }
                    else if (!dependency.Optional && _parentManager?.GetManager(dependency.DependencyType) == null)
                        _log.Warn("Unable to satisfy dependency {0} ({1}) of {2}.", dependency.DependencyType.Name,
                            dependency.Field.Name, manager.Instance.GetType().Name);
                }
                manager.UnsolvedDependencies = inFactor;
                dagQueue.Add(manager);
            }
            #endregion

            // Sorts the dependency graph into _orderedManagers
            #region Sort
            var tmpQueue = new List<ManagerInstance>();
            while (dagQueue.Any())
            {
                tmpQueue.Clear();
                for (var i = 0; i < dagQueue.Count; i++)
                {
                    if (dagQueue[i].UnsolvedDependencies == 0)
                        tmpQueue.Add(dagQueue[i]);
                    else
                        dagQueue[i - tmpQueue.Count] = dagQueue[i];
                }
                dagQueue.RemoveRange(dagQueue.Count - tmpQueue.Count, tmpQueue.Count);
                if (tmpQueue.Count == 0)
                {
                    _log.Fatal("Dependency loop detected in the following managers:");
                    foreach (ManagerInstance manager in dagQueue)
                    {
                        _log.Fatal("   + {0} has {1} unsolved dependencies.", manager.Instance.GetType().FullName, manager.UnsolvedDependencies);
                        _log.Fatal("        - Dependencies: {0}",
                            string.Join(", ", manager.Dependencies.Select(x => x.DependencyType.Name + (x.Optional ? " (Optional)" : ""))));
                        _log.Fatal("        - Dependents: {0}",
                            string.Join(", ", manager.Dependents.Select(x => x.Instance.GetType().Name)));
                    }
                    throw new InvalidOperationException("Unable to satisfy all required manager dependencies");
                }
                // Update the number of unsolved dependencies
                foreach (ManagerInstance manager in tmpQueue)
                    foreach (ManagerInstance dependent in manager.Dependents)
                        dependent.UnsolvedDependencies--;
                // tmpQueue.Sort(); If we have priorities this is where to sort them.
                _orderedManagers.AddRange(tmpQueue);
            }
            _log.Debug("Dependency tree satisfied.  Load order is:");
            foreach (ManagerInstance manager in _orderedManagers)
            {
                _log.Debug("   - {0}", manager.Instance.GetType().FullName);
                _log.Debug("        - Dependencies: {0}",
                    string.Join(", ", manager.Dependencies.Select(x => x.DependencyType.Name + (x.Optional ? " (Optional)" : ""))));
                _log.Debug("        - Dependents: {0}",
                    string.Join(", ", manager.Dependents.Select(x => x.Instance.GetType().Name)));
            }

            #endregion

            // Updates the dependency fields with the correct manager instances
            #region Satisfy
            foreach (ManagerInstance manager in _registeredManagers)
            {
                manager.Dependents.Clear();
                foreach (DependencyInfo dependency in manager.Dependencies)
                    dependency.Field.SetValue(manager.Instance, GetManager(dependency.DependencyType));
            }
            #endregion
        }

        private bool _initialized = false;

        /// <summary>
        /// Initializes the dependency manager, and all its registered managers.
        /// </summary>
        public void Init()
        {
            if (_initialized)
                throw new InvalidOperationException("Can't start the dependency manager more than once");
            _initialized = true;
            Sort();
            foreach (ManagerInstance manager in _orderedManagers)
                manager.Instance.Init();
        }

        /// <summary>
        /// Disposes the dependency manager, and all its registered managers.
        /// </summary>
        public void Dispose()
        {
            if (!_initialized)
                throw new InvalidOperationException("Can't dispose an uninitialized dependency manager");
            for (int i = _orderedManagers.Count - 1; i >= 0; i--)
            {
                _orderedManagers[i].Instance.Dispose();
                foreach (DependencyInfo field in _orderedManagers[i].Dependencies)
                    field.Field.SetValue(_orderedManagers[i].Instance, null);
            }
            _initialized = false;
        }


        /// <summary>
        /// Gets the manager that provides the given type.  If there is no such manager, returns null.
        /// </summary>
        /// <param name="type">Type of manager</param>
        /// <returns>manager, or null if none exists</returns>
        public IManager GetManager(Type type)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (_dependencySatisfaction.TryGetValue(type, out ManagerInstance mgr))
                return mgr.Instance;
            return _parentManager?.GetManager(type);
        }

        /// <summary>
        /// Gets the manager that provides the given type.  If there is no such manager, returns null.
        /// </summary>
        /// <typeparam name="T">Type of manager</typeparam>
        /// <returns>manager, or null if none exists</returns>
        public T GetManager<T>() where T : class, IManager
        {
            return (T)GetManager(typeof(T));
        }
    }
}
