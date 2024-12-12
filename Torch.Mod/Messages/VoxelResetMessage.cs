using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Voxels;

namespace Torch.Mod.Messages
{
    [ProtoContract]
    public class VoxelResetMessage : MessageBase
    {
        [ProtoMember(201)]
        public long[] EntityId;

        public VoxelResetMessage()
        { }

        public VoxelResetMessage(long[] entityId)
        {
            EntityId = entityId;
        }

        public override void ProcessClient()
        {
            //MyAPIGateway.Parallel.ForEach(EntityId, id =>
            foreach (var id in EntityId)
            {
                IMyEntity e;
                if (!MyAPIGateway.Entities.TryGetEntityById(id, out e))
                    continue;

                var v = e as IMyVoxelBase;
                v?.Storage.Reset(MyStorageDataTypeFlags.All);
            }
        }

        public override void ProcessServer()
        {
            throw new Exception();
        }
    }
}