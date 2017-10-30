using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Torch.API;
using VRage.Game.Components;
using VRage.ModAPI;

namespace Torch.Managers.Profiler
{
    public class ProfilerManager : Manager
    {
        public ProfilerManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        /// <summary>
        /// Profile grid related updates.
        /// </summary>
        public bool ProfileGridsUpdate
        {
            get => ProfilerData.ProfileGridsUpdate;
            set => ProfilerData.ProfileGridsUpdate = value;
        }

        /// <summary>
        /// Profile block updates.  Requires <see cref="ProfileGridsUpdate"/>
        /// </summary>
        public bool ProfileBlocksUpdate
        {
            get => ProfilerData.ProfileBlocksUpdate;
            set => ProfilerData.ProfileBlocksUpdate = value;
        }

        /// <summary>
        /// Profile entity component updates.
        /// </summary>
        public bool ProfileEntityComponentsUpdate
        {
            get => ProfilerData.ProfileEntityComponentsUpdate;
            set => ProfilerData.ProfileEntityComponentsUpdate = value;
        }

        /// <summary>
        /// Profile grid system updates.  Requires <see cref="ProfileGridsUpdate"/>
        /// </summary>
        public bool ProfileGridSystemUpdates
        {
            get => ProfilerData.ProfileGridSystemUpdates;
            set => ProfilerData.ProfileGridSystemUpdates = value;
        }

        /// <summary>
        /// Profile session component updates.
        /// </summary>
        public bool ProfileSessionComponentsUpdate
        {
            get => ProfilerData.ProfileSessionComponentsUpdate;
            set => ProfilerData.ProfileSessionComponentsUpdate = value;
        }

        /// <summary>
        /// Gets the profiler information associated with the given entity.
        /// </summary>
        /// <param name="entity">Entity to get information for</param>
        /// <param name="cache">View model to reuse, or null to create a new one</param>
        /// <returns>Information</returns>
        public ProfilerEntryViewModel EntityData(IMyEntity entity, ProfilerEntryViewModel cache = null)
        {
            cache = ProfilerData.BindView(cache);
            cache.SetTarget(entity);
            return cache;
        }


        /// <summary>
        /// Gets the profiler information associated with the given cube grid system.
        /// </summary>
        /// <param name="grid">Cube grid to query</param>
        /// <param name="cubeGridSystem">Cube grid system to query</param>
        /// <param name="cache">View model to reuse, or null to create a new one</param>
        /// <returns>Information</returns>
        public ProfilerEntryViewModel GridSystemData(MyCubeGrid grid, object cubeGridSystem, ProfilerEntryViewModel cache = null)
        {
            cache = ProfilerData.BindView(cache);
            cache.SetTarget(grid, cubeGridSystem);
            return cache;
        }


        /// <summary>
        /// Gets the profiler information associated with the given entity component
        /// </summary>
        /// <param name="component">Component to get information for</param>
        /// <param name="cache">View model to reuse, or null to create a new one</param>
        /// <returns>Information</returns>
        public ProfilerEntryViewModel EntityComponentData(MyEntityComponentBase component, ProfilerEntryViewModel cache = null)
        {
            cache = ProfilerData.BindView(cache);
            cache.SetTarget(component);
            return cache;
        }

        /// <summary>
        /// Gets the profiler information associated with all entities
        /// </summary>
        /// <param name="cache">View model to reuse, or null to create a new one</param>
        /// <returns>View model</returns>
        public ProfilerEntryViewModel EntitiesData(ProfilerEntryViewModel cache = null)
        {
            cache = ProfilerData.BindView(cache);
            cache.SetTarget(ProfilerFixedEntry.Entities);
            return cache;
        }

        /// <summary>
        /// Gets the profiler information associated with the session
        /// </summary>
        /// <param name="cache">View model to reuse, or null to create a new one</param>
        /// <returns>View model</returns>
        public ProfilerEntryViewModel SessionData(ProfilerEntryViewModel cache = null)
        {
            cache = ProfilerData.BindView(cache);
            cache.SetTarget(ProfilerFixedEntry.Session);
            return cache;
        }
    }
}
