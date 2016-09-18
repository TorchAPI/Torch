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
using SteamSDK;

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
            ServerManager.Static.SessionReady += InitChatHandler;
        }

        public void InitChatHandler()
        {
            MyMultiplayer.Static.ChatMessageReceived += MessageReceived;
        }

        public void MessageReceived(ulong steamId, string message, ChatEntryTypeEnum chatType)
        {
            //Messages sent from server loop back around.
            if (steamId == MyMultiplayer.Static.ServerId)
                return;

            var name = MyMultiplayer.Static.GetMemberName(steamId);
            Dispatcher.Invoke(() => AddMessage(name, message), DispatcherPriority.Normal);
        }

        public void AddMessage(string sender, string message)
        {
            Chat.Text += $"{DateTime.Now.ToLongTimeString()} | {sender}: {message}\n";
            Program.UserInterface.Players.RefreshNames();
        }

        public void SendMessage(string message)
        {
            MyMultiplayer.Static.SendChatMessage(message);
            Dispatcher.Invoke(() => AddMessage("Server", message));
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            OnMessageEntered();
        }

        private void OnMessageEntered()
        {
            var text = Message.Text;
            SendMessage(text);
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
