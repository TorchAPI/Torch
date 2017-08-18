using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API.Managers
{
    /// <summary>
    /// Base interface for Torch managers.
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Initializes the manager. Called once this manager's dependencies have been initialized.
        /// </summary>
        void Init();

        /// <summary>
        /// Disposes the manager.  Called before this manager's dependencies are disposed.
        /// </summary>
        void Dispose();
    }
}
