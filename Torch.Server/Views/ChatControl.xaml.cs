using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using NLog;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Server.Managers;
using Torch.Server.Views;
using VRage.Game;
using Color = VRageMath.Color;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
#pragma warning disable CS0618
        private ITorchServer _server = (ITorchServer) TorchBase.Instance;
#pragma warning restore CS0618
        private readonly LinkedList<string> _lastMessages = new();
        private LinkedListNode<string> _currentLastMessageNode;

        public ChatControl()
        {
            InitializeComponent();
            this.IsVisibleChanged += OnIsVisibleChanged;
            MessageBox.Provider = new CommandSuggestionsProvider(_server);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                //I hate this and I hate myself. You should hate me too
                Task.Run(() =>
                {
                    Thread.Sleep(100);

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Focus();
                        Keyboard.Focus(MessageBox);
                    });
                });
            }
        }

        public void BindServer(ITorchServer server)
        {
            _server = server;

            server.Initialized += Server_Initialized            ;
        }

        private void Server_Initialized(ITorchServer obj)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ChatItems.Inlines.Clear();
            });

            var sessionManager = _server.Managers.GetManager<ITorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionStateChanged;
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loading:
                    Dispatcher.InvokeAsync(() => ChatItems.Inlines.Clear());
                    break;
                case TorchSessionState.Loaded:
                    {
                        var chatMgr = session.Managers.GetManager<IChatManagerClient>();
                        if (chatMgr != null)
                            chatMgr.MessageRecieved += OnMessageRecieved;
                    }
                    break;
                case TorchSessionState.Unloading:
                    {
                        var chatMgr = session.Managers.GetManager<IChatManagerClient>();
                        if (chatMgr != null)
                            chatMgr.MessageRecieved -= OnMessageRecieved;
                    }
                    break;
                case TorchSessionState.Unloaded:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void OnMessageRecieved(TorchChatMessage msg, ref bool consumed)
        {
            InsertMessage(msg);
        }

        private static readonly Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        private static Brush LookupBrush(string font)
        {
            if (_brushes.TryGetValue(font, out Brush result))
                return result;
            Brush brush = typeof(Brushes).GetField(font, BindingFlags.Static)?.GetValue(null) as Brush ?? Brushes.Blue;
            _brushes.Add(font, brush);
            return brush;
        }

        private void InsertMessage(TorchChatMessage msg)
        {
            if (Dispatcher.CheckAccess())
            {
                bool atBottom = ChatScroller.VerticalOffset + 8 > ChatScroller.ScrollableHeight;
                var span = new Span();
                span.Inlines.Add($"{msg.Timestamp} ");
                switch (msg.Channel)
                {
                    case ChatChannel.Faction:
                        span.Inlines.Add(new Run($"[{MySession.Static.Factions.TryGetFactionById(msg.Target)?.Tag ?? "???"}] ") { Foreground = Brushes.Green });
                        break;
                    case ChatChannel.Private:
                        span.Inlines.Add(new Run($"[to {MySession.Static.Players.TryGetIdentity(msg.Target)?.DisplayName ?? "???"}] ") { Foreground = Brushes.DeepPink });
                        break;
                }
                span.Inlines.Add(new Run(msg.Author) { Foreground = LookupBrush(msg.Font) });
                span.Inlines.Add($": {msg.Message}");
                span.Inlines.Add(new LineBreak());
                ChatItems.Inlines.Add(span);
                if (atBottom)
                    ChatScroller.ScrollToBottom();
            }
            else
                Dispatcher.InvokeAsync(() => InsertMessage(msg));
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            OnMessageEntered();
        }

        private void Message_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    OnMessageEntered();
                    break;
                case Key.Up:
                    _currentLastMessageNode = _currentLastMessageNode?.Previous ?? _lastMessages.Last;
                    MessageBox.Text = _currentLastMessageNode?.Value ?? string.Empty;
                    break;
                case Key.Down:
                    _currentLastMessageNode = _currentLastMessageNode?.Next ?? _lastMessages.First;
                    MessageBox.Text = _currentLastMessageNode?.Value ?? string.Empty;
                    break;
            }
        }

        private void OnMessageEntered()
        {
            //Can't use Message.Text directly because of object ownership in WPF.
            var text = MessageBox.Text;
            if (string.IsNullOrEmpty(text))
                return;

            var commands = _server.CurrentSession?.Managers.GetManager<Torch.Commands.CommandManager>();
            if (commands != null && commands.IsCommand(text))
            {
                InsertMessage(new(_server.Config.ChatName, text, Color.Red, _server.Config.ChatColor));
                _server.Invoke(() =>
                {
                    if (commands.HandleCommandFromServer(text, InsertMessage)) return;
                    InsertMessage(new(_server.Config.ChatName, "Invalid command.", Color.Red, _server.Config.ChatColor));
                });
            }
            else
            {
                _server.CurrentSession?.Managers.GetManager<IChatManagerClient>().SendMessageAsSelf(text);
            }
            if (_currentLastMessageNode is { } && _currentLastMessageNode.Value == text)
            {
                _lastMessages.Remove(_currentLastMessageNode);
            }
            _lastMessages.AddLast(text);
            _currentLastMessageNode = null;
            MessageBox.Text = "";
        }
    }
}
