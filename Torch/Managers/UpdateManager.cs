using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamSDK;

namespace Torch.Managers
{
    /// <summary>
    /// Handles updating of the DS and Torch plugins.
    /// </summary>
    public class UpdateManager
    {
        private Timer _updatePollTimer;

        public UpdateManager()
        {
            _updatePollTimer = new Timer(CheckForUpdates, this, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        private void CheckForUpdates(object state)
        {
            
        }
    }
}
