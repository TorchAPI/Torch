using System.Windows.Controls;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Managers.Profiler;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Torch.Server.ViewModels.Entities
{
    public class EntityViewModel : ViewModel
    {
        protected EntityTreeViewModel Tree { get; }
        public IMyEntity Entity { get; }
        public long Id => Entity.EntityId;
        public ProfilerEntryViewModel Profiler
        {
            get => ProfilerTreeAlias[0];
            set => ProfilerTreeAlias[0] = value;
        }
        public MtObservableList<ProfilerEntryViewModel> ProfilerTreeAlias { get; } = new MtObservableList<ProfilerEntryViewModel>(1){null};

        public virtual string Name
        {
            get => Entity.DisplayName;
            set
            {
                TorchBase.Instance.InvokeBlocking(() => Entity.DisplayName = value);
                OnPropertyChanged();
            }
        }

        public virtual string Position
        {
            get => Entity.GetPosition().ToString();
            set
            {
                if (!Vector3D.TryParse(value, out Vector3D v))
                    return;

                TorchBase.Instance.InvokeBlocking(() => Entity.SetPosition(v));
                OnPropertyChanged();
            }
        }

        public virtual bool CanStop => Entity.Physics?.Enabled ?? false;

        public virtual bool CanDelete => !(Entity is IMyCharacter);

        public virtual void Delete()
        {
            Entity.Close();
        }

        public EntityViewModel(IMyEntity entity, EntityTreeViewModel tree)
        {
            Entity = entity;
            Tree = tree;
            Profiler = TorchBase.Instance.Managers.GetManager<ProfilerManager>()?.EntityData(entity, Profiler);
        }

        public EntityViewModel()
        {
            
        }
    }
}
