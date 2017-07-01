using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using Torch.API;
using Torch.Managers;
using Torch.ViewModels;
using VRage.Game.ModAPI;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for PlayerListControl.xaml
    /// </summary>
    public partial class PlayerListControl : UserControl
    {
        private ITorchServer _server;

        public PlayerListControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            _server = server;
            DataContext = (MultiplayerManager)_server.Multiplayer;
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>)PlayerList.SelectedItem;
            _server.Multiplayer.KickPlayer(player.Key);
        }

        private void BanButton_Click(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>) PlayerList.SelectedItem;
            _server.Multiplayer.BanPlayer(player.Key);
        }
    }
}
