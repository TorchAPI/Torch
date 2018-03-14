using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.World;
using Torch.API.Managers;

namespace Torch.API.Session
{
    /// <summary>
    /// Represents the Torch code working with a single game session
    /// </summary>
    public interface ITorchSession
    {
        /// <summary>
        /// The Torch instance this session is bound to
        /// </summary>
        ITorchBase Torch { get; }

        /// <summary>
        /// The Space Engineers game session this session is bound to.
        /// </summary>
        MySession KeenSession { get; }

        /// <inheritdoc cref="IDependencyManager"/>
        IDependencyManager Managers { get; }

        /// <summary>
        /// The current state of the session
        /// </summary>
        TorchSessionState State { get; }

        /// <summary>
        /// Event raised when the <see cref="State"/> changes.
        /// </summary>
        event TorchSessionStateChangedDel StateChanged;
    }
}
