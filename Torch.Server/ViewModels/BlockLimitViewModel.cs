using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VRage.Game;

namespace Torch.Server.ViewModels
{
    public class BlockLimitViewModel : ViewModel
    {
        private SessionSettingsViewModel _sessionSettings;

        public string BlockType { get; set; }
        public short Limit { get; set; }

        //public CommandBinding Delete { get; } = new CommandBinding(new DeleteCommand());

        public BlockLimitViewModel(SessionSettingsViewModel sessionSettings, string blockType, short limit)
        {
            _sessionSettings = sessionSettings;
            BlockType = blockType;
            Limit = limit;
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
