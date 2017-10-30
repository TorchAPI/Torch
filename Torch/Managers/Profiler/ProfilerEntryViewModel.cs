using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NLog;
using Torch.Collections;
using Torch.Utils;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Torch.Managers.Profiler
{
    public class ProfilerEntryViewModel : INotifyPropertyChanged
    {
#pragma warning disable 649
        [ReflectedGetter(Name = "Keys")]
        private static readonly Func<ConditionalWeakTable<object, SlimProfilerEntry>, ICollection<object>> _weakTableKeys;
#pragma warning restore 649

        public string OwnerName { get; private set; } = "";
        public double UpdateTime { get; private set; }
        public double UpdateTimeMs => 1000 * UpdateTime;
        public double UpdateLoadPercent => 100 * UpdateTime / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

        public MtObservableList<ProfilerEntryViewModel> Children { get; } = new MtObservableList<ProfilerEntryViewModel>();

        private ProfilerFixedEntry _fixedEntry = ProfilerFixedEntry.Count;
        private readonly WeakReference<object> _owner = new WeakReference<object>(null);
        private WeakReference<object>[] _getterExtra;
        private Func<SlimProfilerEntry> _getter;

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        #region Getter Impls
        private SlimProfilerEntry GetterImplEntity()
        {
            return _owner.TryGetTarget(out var own) ? ProfilerData.EntityEntry((IMyEntity)own) : null;
        }

        private SlimProfilerEntry GetterImplEntityComponent()
        {
            return _owner.TryGetTarget(out var own)
                ? ProfilerData.EntityComponentEntry((MyEntityComponentBase)own)
                : null;
        }

        private SlimProfilerEntry GetterImplGridSystem()
        {
            return _owner.TryGetTarget(out var own) && _getterExtra[0].TryGetTarget(out object grd) ? ProfilerData.GridSystemEntry(own, (IMyCubeGrid)grd) : null;
        }
        #endregion

        #region SetTarget

        internal void SetTarget(ProfilerFixedEntry owner)
        {
            if (owner == ProfilerFixedEntry.Count)
                throw new ArgumentOutOfRangeException(nameof(owner), "Must not be the count enum");
            _fixedEntry = owner;
            _owner.SetTarget(null);
            _getterExtra = null;
            // we can capture here since its a value type
            _getter = () => ProfilerData.FixedProfiler(owner);
        }

        internal void SetTarget(IMyEntity owner)
        {
            _fixedEntry = ProfilerFixedEntry.Count;
            _owner.SetTarget(owner);
            _getterExtra = new WeakReference<object>[0];
            _getter = GetterImplEntity;
        }

        internal void SetTarget(MyEntityComponentBase owner)
        {
            _fixedEntry = ProfilerFixedEntry.Count;
            _owner.SetTarget(owner);
            _getterExtra = new WeakReference<object>[0];
            _getter = GetterImplEntityComponent;
        }

        internal void SetTarget(IMyCubeGrid grid, object owner)
        {
            _fixedEntry = ProfilerFixedEntry.Count;
            _owner.SetTarget(owner);
            _getterExtra = new[] { new WeakReference<object>(grid) };
            _getter = GetterImplGridSystem;
        }
        #endregion

        /// <summary>
        /// Called to update the values of this view model without changing the target.
        /// </summary>
        /// <returns>False if the target was lost</returns>
        internal bool Update()
        {
            object owner;
            if (_fixedEntry == ProfilerFixedEntry.Count)
            {
                bool lostHandle = !_owner.TryGetTarget(out owner);
                if (_getterExtra != null && !lostHandle)
                    foreach (WeakReference<object> ext in _getterExtra)
                        if (!ext.TryGetTarget(out _))
                        {
                            lostHandle = true;
                            break;
                        }
                if (lostHandle)
                {
                    OwnerName = "Lost Handle";
                    OnPropertyChanged(nameof(OwnerName));
                    UpdateTime = 0;
                    OnPropertyChanged(nameof(UpdateTime));
                    Children.Clear();
                    return false;
                }
            }
            else
                owner = _fixedEntry;
            UpdateInternal(owner, _getter());
            return true;
        }

        private const string _noData = "No Data";

        private void UpdateInternal(object owner, SlimProfilerEntry entry)
        {
            if (entry == null)
            {
                if (!OwnerName.Equals(_noData))
                {
                    OwnerName = _noData;
                    OnPropertyChanged(nameof(OwnerName));
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (UpdateTime != 0)
                {
                    UpdateTime = 0;
                    OnPropertyChanged(nameof(UpdateTime));
                }
                if (Children.Count > 0)
                    Children.Clear();
                return;
            }
            string ownerId = ProfilerObjectIdentifier.Identify(owner);
            if (!ownerId.Equals(OwnerName))
            {
                OwnerName = ownerId;
                OnPropertyChanged(nameof(OwnerName));
            }
            UpdateTime = entry.UpdateTime;
            OnPropertyChanged(nameof(UpdateTime));

            if (entry is FatProfilerEntry fat)
            {
                ICollection<object> keys = _weakTableKeys(fat.ChildUpdateTime);
                using (Children.DeferredUpdate())
                {
                    while (Children.Count > keys.Count)
                        Children.RemoveAt(Children.Count - 1);
                    var id = 0;
                    foreach (object key in keys)
                    {
                        if (fat.ChildUpdateTime.TryGetValue(key, out SlimProfilerEntry child))
                        {
                            if (id >= Children.Count)
                            {
                                var result = new ProfilerEntryViewModel();
                                result.UpdateInternal(key, child);
                                Children.Add(result);
                                id++;
                            }
                            else
                            {
                                Children[id++].UpdateInternal(key, child);
                            }
                        }
                    }
                    Children.Sort(x => (int) (-x.UpdateTime * 1e6));
                }
            }
            else
            {
                Children.Clear();
            }
        }

        private readonly MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler> _propertyChangedEvent =
            new MtObservableEvent<PropertyChangedEventArgs, PropertyChangedEventHandler>();

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChangedEvent.Add(value);
            remove => _propertyChangedEvent.Remove(value);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(UpdateTime))
            {
                OnPropertyChanged(nameof(UpdateLoadPercent));
                OnPropertyChanged(nameof(UpdateTimeMs));
            }
            _propertyChangedEvent.Raise(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
