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
using System.Windows.Threading;
using Piston;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;

namespace PistonServer
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        public event Action<string> MessageEntered;
        public ChatControl()
        {
            InitializeComponent();
        }

        public void MessageReceived(ulong steamId, string message, SteamSDK.ChatEntryTypeEnum chatType)
        {
            //Messages sent from server loop back around.
            if (steamId == MyMultiplayer.Static.ServerId)
                return;

            var name = MySession.Static.Players.TryGetPlayerBySteamId(steamId)?.DisplayName ?? "";
            Dispatcher.Invoke(() => AddMessage(name, message), DispatcherPriority.Normal);
        }

        public void AddMessage(string sender, string message)
        {
            Chat.Text += $"{DateTime.Now.ToLongTimeString()} | {sender}: {message}\n";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            OnMessageEntered();
            
        }

        private void OnMessageEntered()
        {
            var text = Message.Text;
            AddMessage("Server", text);
            MySandboxGame.Static.Invoke(() => MyMultiplayer.Static.SendChatMessage(text));
            MessageEntered?.Invoke(Message.Text);
            Message.Text = "";
        }

        private void Message_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnMessageEntered();
        }
    }
}
