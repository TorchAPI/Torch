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

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        public ChatControl()
        {
            InitializeComponent();
            ChatItems.ItemsSource = TorchServer.Multiplayer.ChatView;
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
            TorchServer.Multiplayer.SendMessage(text);
            Message.Text = "";
        }
    }
}
