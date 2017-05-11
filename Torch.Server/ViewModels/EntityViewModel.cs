using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Torch.Server.ViewModels
{
    public class EntityViewModel : ViewModel
    {
        public IMyEntity Entity { get; }
        public long Id => Entity.EntityId;

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

        public EntityViewModel(IMyEntity entity)
        {
            Entity = entity;
        }
    }
}
