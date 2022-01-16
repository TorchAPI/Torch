using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Torch.Mod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TorchModCore : MySessionComponentBase
    {
        public const ulong MOD_ID = 2722000298;
        private static bool _init;
        public static bool Debug;

        public override void UpdateAfterSimulation()
        {
            if (_init)
                return;

            _init = true;
            ModCommunication.Register();
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
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
