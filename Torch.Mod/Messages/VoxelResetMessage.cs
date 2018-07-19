using System;
using System.Collections.Generic;
using System.Text;
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
            MyAPIGateway.Parallel.ForEach(EntityId, id =>
                                                      {
                                                          IMyEntity e;
                                                          if (!MyAPIGateway.Entities.TryGetEntityById(id, out e))
                                                              return;

                                                          var v = e as IMyVoxelBase;
                                                          if (v == null)
                                                              return;

                                                          v.Storage.Reset(MyStorageDataTypeFlags.All);
                                                      });
        }

        public override void ProcessServer()
        {
            throw new Exception();
        }
    }
}
