using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VRage.Plugins;

namespace Torch.API
{
    public interface ITorchPlugin : IPlugin
    {
        void Init(ITorchServer server);
        void Reload();
    }
}
