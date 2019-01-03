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
using NLog;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Server.Managers;
using Torch.Utils;
using Torch.ViewModels;
using VRage.Game.ModAPI;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for PlayerListControl.xaml
    /// </summary>
    public partial class PlayerListControl : UserControl
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private ITorchServer _server;
        private IMultiplayerManagerServer _mpServer;

        public PlayerListControl()
        {
            InitializeComponent();
        }

        private void OnPlayerPromoted(ulong arg1, MyPromoteLevel arg2)
        {
            Dispatcher.InvokeAsync(() => PlayerList.Items.Refresh());
        }

        public void BindServer(ITorchServer server)
        {
            _server = server;
            _server.Initialized += Server_Initialized;
        }

        private void Server_Initialized(ITorchServer obj)
        {
            var sessionManager = _server.Managers.GetManager<ITorchSessionManager>();
            sessionManager.SessionStateChanged += SessionStateChanged;
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    Dispatcher.InvokeAsync(() => DataContext = _server?.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>());
                    _mpServer = _server.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>();
                    _mpServer.PlayerPromoted += OnPlayerPromoted;
                    break;
                case TorchSessionState.Unloading:
                    Dispatcher.InvokeAsync(() => DataContext = null);
                    break;
            }
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>)PlayerList.SelectedItem;
            try
            {
                _server.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>().KickPlayer(player.Key);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        private void BanButton_Click(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>)PlayerList.SelectedItem;
            try
            {
                _server.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>().BanPlayer(player.Key);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        private void PromoteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>)PlayerList.SelectedItem;
            try
            {
                _server.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>().PromoteUser(player.Key);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        private void DemoteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var player = (KeyValuePair<ulong, PlayerViewModel>)PlayerList.SelectedItem;
            try
            {
                _server.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>().DemoteUser(player.Key);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }
    }
}
