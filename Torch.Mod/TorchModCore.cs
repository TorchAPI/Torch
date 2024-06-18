using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Torch.Mod.Messages;
using VRage.Game.Components;
using VRage.Utils;

namespace Torch.Mod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TorchModCore : MySessionComponentBase
    {

        public const ulong MOD_ID = 3270275515; //real
        //public const ulong MOD_ID = 2916923149; //old
        private static bool _init;
        public static bool Debug;
        public static MyStringId id;

        public override void UpdateAfterSimulation()
        {
            if (_init)
                return;

            _init = true;
            ModCommunication.Register();
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            id = MyStringId.GetOrCompute("Square");

        }

        public override void Draw()
        {
            DrawDebug.refreshAllDraws();
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText == "@!debug")
            {
                Debug = !Debug;
                MyAPIGateway.Utilities.ShowMessage("Torch", $"Debug: {Debug}");
                sendToOthers = false;
            }
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
                ModCommunication.Unregister();
            }
            catch
            {
                //session unloading, don't care
            }
        }
    }
}