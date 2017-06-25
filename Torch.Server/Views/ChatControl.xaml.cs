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
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using Torch.API;
using Torch.Managers;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        private TorchBase _server;
        private MultiplayerManager _multiplayer;

        public ChatControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            _server = (TorchBase)server;
            _multiplayer = (MultiplayerManager)server.Multiplayer;
            DataContext = _multiplayer;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            OnMessageEntered();
        }

        private void Message_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnMessageEntered();
        }

        private void OnMessageEntered()
        {
            //Can't use Message.Text directly because of object ownership in WPF.
            var text = Message.Text;
            var commands = _server.Commands;
            string response = null;
            if (commands.IsCommand(text))
            {
                 _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", text));
                _server.InvokeBlocking(() =>
                {
                    response = commands.HandleCommandFromServer(text);
                });
            }
            else
            {
                _server.Multiplayer.SendMessage(text);
            }
            if (!string.IsNullOrEmpty(response))
                _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", response));
            Message.Text = "";
        }
    }
}
