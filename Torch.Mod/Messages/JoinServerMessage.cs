using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Torch.Mod.Messages
{
    [ProtoContract]
    public class JoinServerMessage : MessageBase
    {
        [ProtoMember(201)]
        public int Delay;
        [ProtoMember(202)]
        public string Address;

        private JoinServerMessage()
        {

        }

        public JoinServerMessage(string address)
        {
            Address = address;
        }

        public JoinServerMessage(string address, int delay)
        {
            Address = address;
            Delay = delay;
        }

        public override void ProcessClient()
        {

            MyLog.Default.WriteLineAndConsole($"Torch: Joining server {Address} with delay {Delay}");

            if (Delay <= 0)
            {
                MyAPIGateway.Multiplayer.JoinServer(Address);
                return;
            }

            MyAPIGateway.Parallel.StartBackground(() =>
            {
                MyAPIGateway.Parallel.Sleep(Delay);
                MyAPIGateway.Multiplayer.JoinServer(Address);
            });
        }

        public override void ProcessServer()
        {
        }
    }
}