using Sandbox.Game.Entities.Character;

namespace Torch.Server.ViewModels.Entities
{
    public class CharacterViewModel : EntityViewModel
    {
        public CharacterViewModel(MyCharacter character, EntityTreeViewModel tree) : base(character, tree)
        {
            character.ControllerInfo.ControlAcquired += (x) => { OnPropertyChanged(nameof(Name)); };
            character.ControllerInfo.ControlReleased += (x) => { OnPropertyChanged(nameof(Name)); };
        }

        public CharacterViewModel()
        {
        }
    }
}
