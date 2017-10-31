using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using NLog.Fluent;
using Torch.API;
using Torch.Collections;
using Torch.Managers;
using Torch.Server.ViewModels.Entities;
using Torch.Utils;

using WeakEntityControlFactoryResult = System.Collections.Generic.KeyValuePair<System.WeakReference<Torch.Server.ViewModels.Entities.EntityViewModel>, System.WeakReference<Torch.Server.ViewModels.Entities.EntityControlViewModel>>;

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

        private readonly Dictionary<Delegate, List<WeakEntityControlFactoryResult>> _modelFactories = new Dictionary<Delegate, List<WeakEntityControlFactoryResult>>();
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
                var results = new List<WeakEntityControlFactoryResult>();
                _modelFactories.Add(modelFactory, results);

                var i = 0;
                while (i < _boundEntityViewModels.Count)
                {
                    if (_boundEntityViewModels[i].TryGetTarget(out EntityViewModel target) &&
                        _boundViewModels.TryGetValue(target, out MtObservableList<EntityControlViewModel> components))
                    {
                        if (target is TEntityBaseModel tent)
                        {
                            EntityControlViewModel result = modelFactory.Invoke(tent);
                            if (result != null)
                            {
                                _log.Debug($"Model factory {modelFactory.Method} created {result} for {tent}");
                                components.Add(result);
                                results.Add(new WeakEntityControlFactoryResult(new WeakReference<EntityViewModel>(target), new WeakReference<EntityControlViewModel>(result)));
                            }
                        }
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
                if (!_modelFactories.TryGetValue(modelFactory, out var results))
                    return;
                _modelFactories.Remove(modelFactory);
                foreach (WeakEntityControlFactoryResult result in results)
                {
                    if (result.Key.TryGetTarget(out EntityViewModel target) &&
                        result.Value.TryGetTarget(out EntityControlViewModel created)
                        && _boundViewModels.TryGetValue(target, out MtObservableList<EntityControlViewModel> registered))
                    {
                        registered.Remove(created);
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
                        _log.Debug($"Control factory {factory.Method} created {result}");
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
                foreach (KeyValuePair<Delegate, List<WeakEntityControlFactoryResult>> factory in _modelFactories)
                {
                    Type ptype = factory.Key.Method.GetParameters()[0].ParameterType;
                    if (ptype.IsInstanceOfType(key) &&
                        factory.Key.DynamicInvoke(key) is EntityControlViewModel result)
                    {
                        _log.Debug($"Model factory {factory.Key.Method} created {result} for {key}");
                        binding.Add(result);
                        result.InvalidateControl();
                        factory.Value.Add(new WeakEntityControlFactoryResult(new WeakReference<EntityViewModel>(key), new WeakReference<EntityControlViewModel>(result)));
                    }
                }
                _boundEntityViewModels.Add(new WeakReference<EntityViewModel>(key));
            }
            return binding;
        }
    }
}
