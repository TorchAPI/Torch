using System.Collections.ObjectModel;
using Sandbox.Game.World;
using VRage.Game;

namespace Torch.Server.ViewModels.Entities
{
    public class FactionViewModel : ViewModel
    {
        public FactionViewModel()
        { }
        
        public MyFaction Faction { get; }

        public FactionViewModel(MyFaction faction)
        {
            Faction = faction;
            GenerateMembers();
        }
        
        public string Name => Faction.Name;
        public string Description => Faction.Description;
        public string Tag => Faction.Tag;
        public long ID => Faction.FactionId;

        public MemberData Founder { get; private set; }
        public ObservableCollection<MemberData> Leaders { get; } = new ObservableCollection<MemberData>();
        public ObservableCollection<MemberData> Members { get; } = new ObservableCollection<MemberData>();

        public void GenerateMembers()
        {
            Leaders.Clear();
            Members.Clear();
            foreach (MyFactionMember factionMember in Faction.Members.Values)
            {
                var playerIdent = MySession.Static.Players.TryGetIdentity(factionMember.PlayerId);
                MySession.Static.Players.TryGetPlayerId(factionMember.PlayerId, out MyPlayer.PlayerId playerId);
                MemberData md = new MemberData
                {
                    PlayerIdent = playerIdent,
                    PlayerId = playerId
                };

                if (factionMember.IsFounder)
                {
                    Founder = md;
                    continue;
                }
                
                if (factionMember.IsLeader)
                {
                    Leaders.Add(md);
                    continue;
                }
                
                Members.Add(md);
            }
        }
    }

    public sealed class MemberData
    {
        public MyIdentity PlayerIdent { get; set; }
        public MyPlayer.PlayerId PlayerId { get; set; }
    }
}
