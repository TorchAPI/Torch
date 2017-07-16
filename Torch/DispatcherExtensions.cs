using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Torch
{
    public static class DispatcherExtensions
    {
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return dispatcher.BeginInvoke(priority, action);
        }
    }
}
