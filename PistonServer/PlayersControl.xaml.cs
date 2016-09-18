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
using Piston;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;

namespace PistonServer
{
    /// <summary>
    /// Interaction logic for PlayersControl.xaml
    /// </summary>
    public partial class PlayersControl : UserControl
    {
        public PlayersControl()
        {
            InitializeComponent();
            ServerManager.Static.SessionReady += Static_SessionReady;
        }

        public void RefreshNames()
        {
            Dispatcher.Invoke(() =>
                              {
                                  foreach (var player in PlayerList.Items)
                                  {
                                      var p = (PlayerItem)player;
                                      p.Name = MyMultiplayer.Static.GetMemberName(p.SteamId);
                                  }

                                  PlayerList.Items.Refresh();
                              });
        }

        private void Static_SessionReady()
        {
            MyMultiplayer.Static.ClientKicked += OnClientKicked;
            MyMultiplayer.Static.ClientLeft += OnClientLeft;
            MySession.Static.Players.PlayerRequesting += OnPlayerRequesting;
        }

        /// <summary>
        /// Invoked when a client logs in and hits the respawn screen.
        /// </summary>
        private void OnPlayerRequesting(PlayerRequestArgs args)
        {
            var steamId = args.PlayerId.SteamId;
            var player = new PlayerItem { Name = MyMultiplayer.Static.GetMemberName(steamId), SteamId = steamId };
            Program.UserInterface.Chat.SendMessage($"{player.Name} connected.");
            Dispatcher.Invoke(() => PlayerList.Items.Add(player));
        }
        private void OnClientKicked(ulong steamId)
        {
            OnClientLeft(steamId, ChatMemberStateChangeEnum.Kicked);
        }

        private void OnClientLeft(ulong steamId, ChatMemberStateChangeEnum stateChange)
        {
            Dispatcher.Invoke(() =>
            {
                var player = PlayerList.Items.Cast<PlayerItem>().FirstOrDefault(x => x.SteamId == steamId);

                if (player == null)
                    return;

                Program.UserInterface.Chat.SendMessage($"{player.Name} {stateChange.ToString().ToLower()}.");
                PlayerList.Items.Remove(player);
            });
        }

        public class PlayerItem
        {
            public ulong SteamId;
            public string Name;

            public override string ToString()
            {
                return $"{Name} ({SteamId})";
            }
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
                return;

            var player = (PlayerItem)PlayerList.SelectedItem;
            MyMultiplayer.Static.KickClient(player.SteamId);
        }

        private void BanButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
                return;

            var player = (PlayerItem)PlayerList.SelectedItem;
            MyMultiplayer.Static.BanClient(player.SteamId, true);
        }
    }
}
