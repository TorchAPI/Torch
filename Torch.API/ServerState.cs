using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    /// <summary>
    /// Used to indicate the state of the dedicated server.
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// The server is not running.
        /// </summary>
        Stopped,

        /// <summary>
        /// The server is starting/loading the session.
        /// </summary>
        Starting,

        /// <summary>
        /// The server is running.
        /// </summary>
        Running,

        /// <summary>
        /// The server encountered an error.
        /// </summary>
        Error
    }
}
