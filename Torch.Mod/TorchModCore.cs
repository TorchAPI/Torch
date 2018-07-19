using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Torch.Mod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TorchModCore : MySessionComponentBase
    {
        public const long MOD_ID = 1406994352;
        private static bool _init;

        public override void UpdateAfterSimulation()
        {
            if (_init)
                return;

            _init = true;
            ModCommunication.Register();
        }

        protected override void UnloadData()
        {
            try
            {
                ModCommunication.Unregister();
            }
            catch
            {
                //session unloading, don't care
            }
        }
    }
}
