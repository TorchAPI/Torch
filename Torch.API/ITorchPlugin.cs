using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface ITorchPlugin : IDisposable
    {
        Guid Id { get; }
        Version Version { get; }
        string Name { get; }

        /// <summary>
        /// Called when the game is initialized.
        /// </summary>
        /// <param name="torchBase"></param>
        void Init(ITorchBase torchBase);

        /// <summary>
        /// Called after each game tick. Not thread safe, use invocation methods in <see cref="ITorchBase"/>.
        /// </summary>
        void Update();
    }
}
