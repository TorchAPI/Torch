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
        /// Initializes the manager. Called after Torch is initialized.
        /// </summary>
        void Init();
    }
}
