using System;
using Torch.API;

namespace Torch.ViewModels
{
    public class ChatItem : ViewModel, IChatItem
    {
        private IPlayer _sender;
        private string _message;
        private DateTime _timestamp;

        public IPlayer Player
        {
            get { return _sender; }
            set { _sender = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }
        }

        public DateTime Time
        {
            get { return _timestamp; }
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string TimeString => Time.ToShortTimeString();

        public ChatItem(IPlayer sender, string message, DateTime timestamp = default(DateTime))
        {
            _sender = sender;
            _message = message;

            if (timestamp == default(DateTime))
                _timestamp = DateTime.Now;
            else
                _timestamp = timestamp;
        }
    }
}