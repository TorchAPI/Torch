using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface ITorchPlugin
    {
        Guid Id { get; }
        Version Version { get; }
        string Name { get; }
        bool Enabled { get; set; }

        void Init(ITorchBase torchBase);
        void Update();
        void Unload();
    }
}
