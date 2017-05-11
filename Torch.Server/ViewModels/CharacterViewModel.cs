using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Character;
using VRage.Game.ModAPI;

namespace Torch.Server.ViewModels
{
    public class CharacterViewModel : EntityViewModel
    {
        public CharacterViewModel(MyCharacter character) : base(character)
        {
            
        }
    }
}
