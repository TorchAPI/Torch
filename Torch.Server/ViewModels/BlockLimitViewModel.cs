namespace Torch.Server.ViewModels
{
    public class BlockLimitViewModel : ViewModel
    {
        private SessionSettingsViewModel _sessionSettings;
        private string _blockType;
        private short _limit;

        public string BlockType { get => _blockType; set { _blockType = value; OnPropertyChanged(); } }
        public short Limit { get => _limit; set { _limit = value; OnPropertyChanged(); } }

        //public CommandBinding Delete { get; } = new CommandBinding(new DeleteCommand());

        public BlockLimitViewModel(SessionSettingsViewModel sessionSettings, string blockType, short limit)
        {
            _sessionSettings = sessionSettings;
            _blockType = blockType;
            _limit = limit;
        }

        /* TODO: figure out how WPF commands work
        public class DeleteCommand : ICommand
        {
            /// <inheritdoc />
            public bool CanExecute(object parameter)
            {
                return ((BlockLimitViewModel)parameter)._sessionSettings.BlockLimits.Contains(parameter);
            }

            /// <inheritdoc />
            public void Execute(object parameter)
            {
                ((BlockLimitViewModel)parameter)._sessionSettings.BlockLimits.Remove((BlockLimitViewModel)parameter);
            }

            /// <inheritdoc />
            public event EventHandler CanExecuteChanged;
        }*/
    }
}
