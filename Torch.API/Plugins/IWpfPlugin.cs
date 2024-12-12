using System.Windows.Controls;

namespace Torch.API.Plugins
{
    public interface IWpfPlugin : ITorchPlugin
    {
        /// <summary>
        /// Used by the server's WPF interface to load custom plugin controls.
        /// You must instantiate your plugin's control object here, otherwise it will not be owned by the correct thread for WPF.
        /// </summary>
        UserControl GetControl();
    }
}
