using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PistonAPI
{
    public static class PistonAPI
    {
        public static IServerControls ServerControls { get; }
        public static IEnvironmentInfo EnvironmentInfo { get; }
    }
}
