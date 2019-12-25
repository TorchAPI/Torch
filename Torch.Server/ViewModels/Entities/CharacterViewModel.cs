using Sandbox.Game.Entities.Character;

namespace Torch.Server.ViewModels.Entities
{
    public class CharacterViewModel : EntityViewModel
    {
        private MyCharacter _character;
        public CharacterViewModel(MyCharacter character, EntityTreeViewModel tree) : base(character, tree)
        {
            _character = character;
            character.ControllerInfo.ControlAcquired += ControllerInfo_ControlAcquired;
            character.ControllerInfo.ControlReleased += ControllerInfo_ControlAcquired;
        }

        private void ControllerInfo_ControlAcquired(Sandbox.Game.World.MyEntityController obj)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(CanDelete));
        }

        public CharacterViewModel()
        {
        }

        public override bool CanDelete => _character.ControllerInfo?.Controller?.Player == null;
    }
}
