using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Torch.Mod.Messages
{
    #region Includes
    [ProtoInclude(1, typeof(DialogMessage))]
    [ProtoInclude(2, typeof(NotificationMessage))]
    [ProtoInclude(3, typeof(VoxelResetMessage))]
    [ProtoInclude(4, typeof(JoinServerMessage))]
    [ProtoInclude(5, typeof(DrawDebug))]
    #endregion

    [ProtoContract]
    public abstract class MessageBase
    {
        [ProtoMember(101)]
        public ulong SenderId;

        public abstract void ProcessClient();
        public abstract void ProcessServer();

        //members below not serialized, they're just metadata about the intended target(s) of this message
        internal MessageTarget TargetType;
        internal ulong Target;
        internal ulong[] Ignore;
        internal byte[] CompressedData;


    }
    
    public enum MessageTarget
    {
        /// <summary>
        /// Send to Target
        /// </summary>
        Single,
        /// <summary>
        /// Send to Server
        /// </summary>
        Server,
        /// <summary>
        /// Send to all Clients (only valid from server)
        /// </summary>
        AllClients,
        /// <summary>
        /// Send to all except those steam ID listed in Ignore
        /// </summary>
        AllExcept,
    }
}
