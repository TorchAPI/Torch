using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Torch.API.Plugins
{
    public interface IWpfPlugin : ITorchPlugin
    {
        /// <summary>
        /// Used by the server's WPF interface to load custom plugin controls.
        /// Do not instantiate your plugin control outside of this method! It will throw an exception.
        /// </summary>
        UserControl GetControl();
    }
}
