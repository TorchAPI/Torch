using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Managers.Profiler;

namespace Torch.Server.ViewModels
{
    public class ProfilerViewModel : ViewModel
    {
        public MtObservableList<ProfilerEntryViewModel> ProfilerTreeAlias { get; } = new MtObservableList<ProfilerEntryViewModel>();

        private readonly ProfilerManager _manager;

        public ProfilerViewModel()
        {
            _manager = null;
        }

        public ProfilerViewModel(ProfilerManager profilerManager)
        {
            _manager = profilerManager;
            ProfilerTreeAlias.Add(_manager.SessionData());
            ProfilerTreeAlias.Add(_manager.EntitiesData());
        }

        /// <inheritdoc cref="ProfilerManager.ProfileGridsUpdate"/>
        public bool ProfileGridsUpdate
        {
            get => _manager?.ProfileGridsUpdate ?? false;
            set
            {
                if (_manager != null)
                    _manager.ProfileGridsUpdate = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="ProfilerManager.ProfileBlocksUpdate"/>
        public bool ProfileBlocksUpdate
        {
            get => _manager?.ProfileBlocksUpdate ?? false;
            set
            {
                if (_manager != null)
                    _manager.ProfileBlocksUpdate = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="ProfilerManager.ProfileEntityComponentsUpdate"/>
        public bool ProfileEntityComponentsUpdate
        {
            get => _manager?.ProfileEntityComponentsUpdate ?? false;
            set
            {
                if (_manager != null)
                    _manager.ProfileEntityComponentsUpdate = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="ProfilerManager.ProfileGridSystemUpdates"/>
        public bool ProfileGridSystemUpdates
        {
            get => _manager?.ProfileGridSystemUpdates ?? false;
            set
            {
                if (_manager != null)
                    _manager.ProfileGridSystemUpdates = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="ProfilerManager.ProfileSessionComponentsUpdate"/>
        public bool ProfileSessionComponentsUpdate
        {
            get => _manager?.ProfileSessionComponentsUpdate ?? false;
            set => _manager.ProfileSessionComponentsUpdate = value;
        }
    }
}
