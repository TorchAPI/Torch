using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sandbox.ModAPI;
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.Views.Entities
{
    public partial class FactionView : UserControl
    {
        private readonly FactionViewModel _vm;
        private MemberData _selectedMember;
        private MemberData _selectedLeader;
        
        public FactionView(FactionViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
        }

        private void PromoteLeader(object sender, RoutedEventArgs e)
        {
            if (_selectedLeader is null) return;

            // Need to remove him, or we end up with 2 founders.
            var CurrentFounder = _vm.Founder;
            var NewFounder = _selectedLeader;
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                _vm.Faction.PromoteToFounder(NewFounder.PlayerIdent.IdentityId);
                _vm.Faction.DemoteMember(CurrentFounder.PlayerIdent.IdentityId);
            });
            _selectedLeader = null;
            _vm.GenerateMembers();
        }

        private void PromoteMember(object sender, RoutedEventArgs e)
        {
            if (_selectedMember is null) return;
            var member = _selectedMember;
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.PromoteMember(member.PlayerIdent.IdentityId); });
            _selectedMember = null;
            _vm.GenerateMembers();
        }

        private void DemoteLeader(object sender, RoutedEventArgs e)
        {
            if (_selectedLeader is null) return;
            var member = _selectedLeader;
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.DemoteMember(member.PlayerIdent.IdentityId); });
            _selectedLeader = null;
            _vm.GenerateMembers();
        }

        private void KickLeader(object sender, RoutedEventArgs e)
        {
            if  (_selectedLeader is null) return;
            var member = _selectedLeader;
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.KickMember(member.PlayerIdent.IdentityId); });
            _selectedLeader = null;
            _vm.GenerateMembers();
        }

        private void KickMember(object sender, RoutedEventArgs e)
        {
            if (_selectedMember is null) return;
            var member = _selectedMember;
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.KickMember(member.PlayerIdent.IdentityId); });
            _selectedMember = null;
            _vm.GenerateMembers();
        }

        private void RemoveFaction(object sender, RoutedEventArgs e)
        {
            _selectedLeader = null;
            _selectedMember = null;
            var members = _vm.Faction.Members.Keys.ToList();
            foreach (long factionMember in members)
            {
                if (_vm.Faction.FounderId == factionMember) continue;
                MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.KickMember(factionMember); });
            }
            
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { _vm.Faction.KickMember(_vm.Faction.FounderId); });
            MyAPIGateway.Session.Factions.RemoveFaction(_vm.Faction.FactionId);
        }

        private void NewSelectedMember(object sender, SelectionChangedEventArgs e)
        {
            if (MembersList.SelectedItem is MemberData member) _selectedMember = member;
        }
        
        private void NewSelectedLeader(object sender, SelectionChangedEventArgs e)
        {
            if (LeadersList.SelectedItem is MemberData member) _selectedLeader = member;
        }
    }
}