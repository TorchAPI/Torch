using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.Managers;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        private TorchBase _server;

        public ChatControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            _server = (TorchBase)server;
            ChatItems.Items.Clear();
            server.SessionLoaded += () =>
            {
                var multiplayer = server.CurrentSession.Managers.GetManager<MultiplayerManagerDedicated>();
                DataContext = multiplayer;
                // TODO
                //            if (multiplayer.ChatHistory is INotifyCollectionChanged ncc)
                //                ncc.CollectionChanged += ChatHistory_CollectionChanged;
            };
        }

        private void ChatHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ChatItems.ScrollToItem(ChatItems.Items.Count - 1);
            /*
            if (VisualTreeHelper.GetChildrenCount(ChatItems) > 0)
            {
                
                Border border = (Border)VisualTreeHelper.GetChild(ChatItems, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }*/
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
            if (string.IsNullOrEmpty(text))
                return;

            var commands = _server.CurrentSession?.Managers.GetManager<Torch.Commands.CommandManager>();
            if (commands != null && commands.IsCommand(text))
            {
                // TODO _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", text));
                _server.Invoke(() =>
                {
                    var response = commands.HandleCommandFromServer(text);
                    Dispatcher.BeginInvoke(() => OnMessageEntered_Callback(response));
                });
            }
            else
            {
                _server.CurrentSession?.Managers.GetManager<IChatManagerClient>().SendMessageAsSelf(text);
            }
            Message.Text = "";
        }

        private void OnMessageEntered_Callback(string response)
        {
            //if (!string.IsNullOrEmpty(response))
                // TODO _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", response));
        }
    }
}
