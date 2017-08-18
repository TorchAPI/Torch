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

        public DependencyManager()
        {
            _dependencySatisfaction = new Dictionary<Type, ManagerInstance>();
            _registeredManagers = new List<ManagerInstance>();
            _orderedManagers = new List<ManagerInstance>();
        }

        /// <summary>
        /// Registers the given manager into the dependency system.
        /// </summary>
        /// <remarks>
        /// This method only returns false when there is already a manager registered with a type derived from this given manager,
        /// or when the given manager is derived from an already existing manager.
        /// </remarks>
        /// <param name="manager">Manager to register</param>
        /// <returns>true if added, false if not</returns>
        public bool AddManager(IManager manager)
        {
            // Protect against adding a manager derived from an existing manager
            if (_registeredManagers.Any(x => x.Instance.GetType().IsInstanceOfType(manager)))
                return false;
            // Protect against adding a manager when an existing manager derives from it.
            if (_registeredManagers.Any(x => manager.GetType().IsInstanceOfType(x.Instance)))
                return false;

            var instance = new ManagerInstance(manager);
            _registeredManagers.Add(instance);

            foreach (Type supplied in instance.SuppliedManagers)
                if (_dependencySatisfaction.ContainsKey(supplied))
                    // When we already have a manager supplying this component we have to unregister it.
                    _dependencySatisfaction[supplied] = null;
                else
                    _dependencySatisfaction.Add(supplied, instance);
            return true;
        }

        private void Sort()
        {
            // Resets the previous sort results
            #region Reset
            _orderedManagers.Clear();
            foreach (ManagerInstance manager in _registeredManagers)
            {
                manager.Dependents.Clear();
                foreach (DependencyInfo dependency in manager.Dependencies)
                    dependency.Field.SetValue(manager.Instance, null);
            }
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
                    else if (!dependency.Optional)
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
            #endregion

            // Updates the dependency fields with the correct manager instances
            #region Satisfy
            foreach (ManagerInstance manager in _registeredManagers)
            {
                manager.Dependents.Clear();
                foreach (DependencyInfo dependency in manager.Dependencies)
                    dependency.Field.SetValue(manager.Instance, _dependencySatisfaction.GetValueOrDefault(dependency.DependencyType));
            }
            #endregion
        }

        private bool _initiated = false;

        /// <summary>
        /// Initializes the dependency manager, and all its registered managers.
        /// </summary>
        public void Init()
        {
            if (_initiated)
                throw new InvalidOperationException("Can't start the dependency manager more than once");
            _initiated = true;
            Sort();
            foreach (ManagerInstance manager in _orderedManagers)
                manager.Instance.Init();
        }

        /// <summary>
        /// Gets the manager that provides the given type.  If there is no such manager, returns null.
        /// </summary>
        /// <typeparam name="T">Type of manager</typeparam>
        /// <returns>manager, or null if none exists</returns>
        public T GetManager<T>() where T : class, IManager
        {
            return (T)_dependencySatisfaction.GetValueOrDefault(typeof(T))?.Instance;
        }
    }
}
