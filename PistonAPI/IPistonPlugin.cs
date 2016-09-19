using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VRage.Plugins;

namespace Piston.API
{
    public interface IPistonPlugin : IPlugin
    {
        string Name { get; }
        void Reload();
    }
}
