using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;

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

        public string SteamID
        {
            get
            {
                if (Entity is MyCharacter c)
                    return $"{c.ControlInfo.SteamId}";

                return "nil";
            }
        }
        
        public string GameID
        {
            get
            {
                if (Entity is MyCharacter c)
                    return $"{MySession.Static.Players.TryGetIdentityId(c.ControlInfo.SteamId)}";

                return "nil";
            }
        }
        
        public string LoginTime
        {
            get
            {
                if (!(Entity is MyCharacter c)) return "Unknown?";
                if (c.ControlInfo.SteamId == 0) return "Unknown?";
                
                var player = MySession.Static.Players.TryGetPlayerBySteamId(c.ControlInfo.SteamId);
                return player != null ? player.Identity.LastLoginTime.ToShortTimeString() : "Unknown?";
            }
        }
    }
}
