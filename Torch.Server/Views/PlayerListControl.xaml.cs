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

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for PlayerListControl.xaml
    /// </summary>
    public partial class PlayerListControl : UserControl
    {
        public PlayerListControl()
        {
            InitializeComponent();
            PlayerList.ItemsSource = TorchServer.Multiplayer.PlayersView;
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            var player = PlayerList.SelectedItem as PlayerInfo;
            if (player != null)
            {
                TorchServer.Multiplayer.KickPlayer(player.SteamId);
            }
        }

        private void BanButton_Click(object sender, RoutedEventArgs e)
        {
            var player = PlayerList.SelectedItem as PlayerInfo;
            if (player != null)
            {
                TorchServer.Multiplayer.BanPlayer(player.SteamId);
            }
        }
    }
}
