using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using NLog;
using Torch.API;
using Torch.Collections;
using Torch.Managers;
using Torch.Server.ViewModels.Entities;
using Torch.Utils;

namespace Torch.Server.Managers
{
    /// <summary>
    /// Manager that lets users bind random view models to entities in Torch's Entity Manager
    /// </summary>
    public class EntityControlManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates an entity control manager for the given instance of torch
        /// </summary>
        /// <param name="torchInstance">Torch instance</param>
        internal EntityControlManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        private abstract class ModelFactory
        {
            private readonly ConditionalWeakTable<EntityViewModel, EntityControlViewModel> _models = new ConditionalWeakTable<EntityViewModel, EntityControlViewModel>();

            public abstract Delegate Delegate { get; }

            protected abstract EntityControlViewModel Create(EntityViewModel evm);

#pragma warning disable 649
            [ReflectedGetter(Name = "Keys")]
            private static readonly Func<ConditionalWeakTable<EntityViewModel, EntityControlViewModel>, ICollection<EntityViewModel>> _weakTableKeys;
#pragma warning restore 649

            /// <summary>
            /// Warning: Creates a giant list, avoid if possible.
            /// </summary>
            internal ICollection<EntityViewModel> Keys => _weakTableKeys(_models);

            internal EntityControlViewModel GetOrCreate(EntityViewModel evm)
            {
                return _models.GetValue(evm, Create);
            }

            internal bool TryGet(EntityViewModel evm, out EntityControlViewModel res)
            {
                return _models.TryGetValue(evm, out res);
            }
        }

        private class ModelFactory<T> : ModelFactory where T : EntityViewModel
        {
            private readonly Func<T, EntityControlViewModel> _factory;
            public override Delegate Delegate => _factory;

            internal ModelFactory(Func<T, EntityControlViewModel> factory)
            {
                _factory = factory;
            }


            protected override EntityControlViewModel Create(EntityViewModel evm)
            {
                if (evm is T m)
                {
                    var result = _factory(m);
                    _log.Trace($"Model factory {_factory.Method} created {result} for {evm}");
                    return result;
                }
                return null;
            }
        }

        private readonly List<ModelFactory> _modelFactories = new List<ModelFactory>();
        private readonly List<Delegate> _controlFactories = new List<Delegate>();

        private readonly List<WeakReference<EntityViewModel>> _boundEntityViewModels = new List<WeakReference<EntityViewModel>>();
        private readonly ConditionalWeakTable<EntityViewModel, MtObservableList<EntityControlViewModel>> _boundViewModels = new ConditionalWeakTable<EntityViewModel, MtObservableList<EntityControlViewModel>>();

        /// <summary>
        /// This factory will be used to create component models for matching entity models.
        /// </summary>
        /// <typeparam name="TEntityBaseModel">entity model type to match</typeparam>
        /// <param name="modelFactory">Method to create component model from entity model.</param>
        public void RegisterModelFactory<TEntityBaseModel>(Func<TEntityBaseModel, EntityControlViewModel> modelFactory)
            where TEntityBaseModel : EntityViewModel
        {
            if (!typeof(TEntityBaseModel).IsAssignableFrom(modelFactory.Method.GetParameters()[0].ParameterType))
                throw new ArgumentException("Generic type must match lamda type", nameof(modelFactory));
            lock (this)
            {
                var factory = new ModelFactory<TEntityBaseModel>(modelFactory);
                _modelFactories.Add(factory);

                var i = 0;
                while (i < _boundEntityViewModels.Count)
                {
                    if (_boundEntityViewModels[i].TryGetTarget(out EntityViewModel target) &&
                        _boundViewModels.TryGetValue(target, out MtObservableList<EntityControlViewModel> components))
                    {
                        if (target is TEntityBaseModel tent)
                            UpdateBinding(target, components);
                        i++;
                    }
                    else
                        _boundEntityViewModels.RemoveAtFast(i);
                }
            }
        }

        /// <summary>
        /// Unregisters a factory registered with <see cref="RegisterModelFactory{TEntityBaseModel}"/>
        /// </summary>
        /// <typeparam name="TEntityBaseModel">entity model type to match</typeparam>
        /// <param name="modelFactory">Method to create component model from entity model.</param>
        public void UnregisterModelFactory<TEntityBaseModel>(Func<TEntityBaseModel, EntityControlViewModel> modelFactory)
            where TEntityBaseModel : EntityViewModel
        {
            if (!typeof(TEntityBaseModel).IsAssignableFrom(modelFactory.Method.GetParameters()[0].ParameterType))
                throw new ArgumentException("Generic type must match lamda type", nameof(modelFactory));
            lock (this)
            {
                for (var i = 0; i < _modelFactories.Count; i++)
                {
                    if (_modelFactories[i].Delegate == (Delegate)modelFactory)
                    {
                        foreach (var entry in _modelFactories[i].Keys)
                            if (_modelFactories[i].TryGet(entry, out EntityControlViewModel ecvm) && ecvm != null
                                && _boundViewModels.TryGetValue(entry, out var binding))
                                binding.Remove(ecvm);
                        _modelFactories.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This factory will be used to create controls for matching view models.
        /// </summary>
        /// <typeparam name="TEntityComponentModel">component model to match</typeparam>
        /// <param name="controlFactory">Method to create control from component model</param>
        public void RegisterControlFactory<TEntityComponentModel>(
            Func<TEntityComponentModel, Control> controlFactory)
            where TEntityComponentModel : EntityControlViewModel
        {
            if (!typeof(TEntityComponentModel).IsAssignableFrom(controlFactory.Method.GetParameters()[0].ParameterType))
                throw new ArgumentException("Generic type must match lamda type", nameof(controlFactory));
            lock (this)
            {
                _controlFactories.Add(controlFactory);
                RefreshControls<TEntityComponentModel>();
            }
        }

        ///<summary>
        /// Unregisters a factory registered with <see cref="RegisterControlFactory{TEntityComponentModel}"/>
        /// </summary>
        /// <typeparam name="TEntityComponentModel">component model to match</typeparam>
        /// <param name="controlFactory">Method to create control from component model</param>
        public void UnregisterControlFactory<TEntityComponentModel>(
            Func<TEntityComponentModel, Control> controlFactory)
            where TEntityComponentModel : EntityControlViewModel
        {
            if (!typeof(TEntityComponentModel).IsAssignableFrom(controlFactory.Method.GetParameters()[0].ParameterType))
                throw new ArgumentException("Generic type must match lamda type", nameof(controlFactory));
            lock (this)
            {
                _controlFactories.Remove(controlFactory);
                RefreshControls<TEntityComponentModel>();
            }
        }

        private void RefreshControls<TEntityComponentModel>() where TEntityComponentModel : EntityControlViewModel
        {
            var i = 0;
            while (i < _boundEntityViewModels.Count)
            {
                if (_boundEntityViewModels[i].TryGetTarget(out EntityViewModel target) &&
                    _boundViewModels.TryGetValue(target, out MtObservableList<EntityControlViewModel> components))
                {
                    foreach (EntityControlViewModel component in components)
                        if (component is TEntityComponentModel)
                            component.InvalidateControl();
                    i++;
                }
                else
                    _boundEntityViewModels.RemoveAtFast(i);
            }
        }

        /// <summary>
        /// Gets the models bound to the given entity view model.
        /// </summary>
        /// <param name="entity">view model to query</param>
        /// <returns></returns>
        public MtObservableList<EntityControlViewModel> BoundModels(EntityViewModel entity)
        {
            return _boundViewModels.GetValue(entity, CreateFreshBinding);
        }

        /// <summary>
        /// Gets a control for the given view model type.
        /// </summary>
        /// <param name="model">model to create a control for</param>
        /// <returns>control, or null if none</returns>
        public Control CreateControl(EntityControlViewModel model)
        {
            lock (this)
                foreach (Delegate factory in _controlFactories)
                    if (factory.Method.GetParameters()[0].ParameterType.IsInstanceOfType(model) &&
                        factory.DynamicInvoke(model) is Control result)
                    {
                        _log.Trace($"Control factory {factory.Method} created {result}");
                        return result;
                    }
            _log.Warn($"No control created for {model}");
            return null;
        }

        private MtObservableList<EntityControlViewModel> CreateFreshBinding(EntityViewModel key)
        {
            var binding = new MtObservableList<EntityControlViewModel>();
            lock (this)
            {
                _boundEntityViewModels.Add(new WeakReference<EntityViewModel>(key));
            }
            binding.PropertyChanged += (x, args) =>
            {
                if (nameof(binding.IsObserved).Equals(args.PropertyName))
                    UpdateBinding(key, binding);
            };
            return binding;
        }

        private void UpdateBinding(EntityViewModel key, MtObservableList<EntityControlViewModel> binding)
        {
            if (!binding.IsObserved)
                return;

            lock (this)
            {
                foreach (ModelFactory factory in _modelFactories)
                {
                    var result = factory.GetOrCreate(key);
                    if (result != null && !binding.Contains(result))
                        binding.Add(result);
                }
            }
        }
    }
}
