using System;

namespace Piston.Server.ViewModels
{
    public class ChatItemInfo : ViewModel
    {
        private PlayerInfo _sender;
        private string _message;
        private DateTime _timestamp;

        public PlayerInfo Sender
        {
            get { return _sender; }
            set { _sender = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string Time => Timestamp.ToShortTimeString();

        public ChatItemInfo(PlayerInfo sender, string message)
        {
            _sender = sender;
            _message = message;
            _timestamp = DateTime.Now;
        }
    }
}