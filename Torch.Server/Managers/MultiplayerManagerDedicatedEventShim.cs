using System.Threading.Tasks;
using Torch.API.Event;
using Torch.Event;
using VRage.Network;

namespace Torch.Server.Managers
{
    [EventShim]
    internal static class MultiplayerManagerDedicatedEventShim
    {
        private static readonly EventList<ValidateAuthTicketEvent> _eventValidateAuthTicket =
            new EventList<ValidateAuthTicketEvent>();


        internal static void RaiseValidateAuthTicket(ref ValidateAuthTicketEvent info)
        {
            _eventValidateAuthTicket?.RaiseEvent(ref info);
        }
    }

    /// <summary>
    /// Event that occurs when a player tries to connect to a dedicated server.
    /// Use these values to choose a <see cref="ValidateAuthTicketEvent.FutureVerdict"/>, 
    /// or leave it unset to allow the default logic to handle the request.
    /// </summary>
    public struct ValidateAuthTicketEvent : IEvent
    {
        /// <summary>
        /// SteamID of the player
        /// </summary>
        public readonly ulong SteamID;

        /// <summary>
        /// SteamID of the game owner
        /// </summary>
        public readonly ulong SteamOwner;

        /// <summary>
        /// The response from steam
        /// </summary>
        public readonly JoinResult SteamResponse;

        /// <summary>
        /// ID of the queried group, or <c>0</c> if no group.
        /// </summary>
        public readonly ulong Group;

        /// <summary>
        /// Is this person a member of <see cref="Group"/>.  If no group this is true.
        /// </summary>
        public readonly bool Member;

        /// <summary>
        /// Is this person an officer of <see cref="Group"/>.  If no group this is false.
        /// </summary>
        public readonly bool Officer;

        /// <summary>
        /// A future verdict on this authorization request.  If null, let the default logic choose.  If not async use <see cref="Task.FromResult{TResult}(TResult)"/>
        /// </summary>
        public Task<JoinResult> FutureVerdict;

        internal ValidateAuthTicketEvent(ulong steamId, ulong steamOwner, JoinResult steamResponse,
            ulong serverGroup, bool member, bool officer)
        {
            SteamID = steamId;
            SteamOwner = steamOwner;
            SteamResponse = steamResponse;
            Group = serverGroup;
            Member = member;
            Officer = officer;
            FutureVerdict = null;
        }

        /// <inheritdoc/>
        public bool Cancelled => FutureVerdict != null;
    }
}