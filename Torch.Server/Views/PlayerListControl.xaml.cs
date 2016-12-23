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
using SteamSDK;
using Torch.API;

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
            server.Multiplayer.PlayerJoined += Refresh;
            server.Multiplayer.PlayerLeft += Refresh;
            Refresh();
        }

        private void Refresh(IPlayer player = null)
        {
            Dispatcher.Invoke(() =>
            {
                PlayerList.ItemsSource = null;
                PlayerList.ItemsSource = _server.Multiplayer.Players;
            });
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            var player = PlayerList.SelectedItem as Player;
            if (player != null)
            {
                _server.Multiplayer.KickPlayer(player.SteamId);
            }
        }

        private void BanButton_Click(object sender, RoutedEventArgs e)
        {
            var player = PlayerList.SelectedItem as Player;
            if (player != null)
            {
                _server.Multiplayer.BanPlayer(player.SteamId);
            }
        }
    }
}
