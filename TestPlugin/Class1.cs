using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using VRage.Plugins;

namespace TestPlugin
{
    public class Plugin : TorchPluginBase
    {
        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            //Torch.Log.Write($"Plugin init {Name}");
        }

        /// <inheritdoc />
        public override void Update()
        {
            //Torch.Log.Write($"Plugin update {Name}");
        }

        /// <inheritdoc />
        public override void Unload()
        {
            //Torch.Log.Write($"Plugin unload {Name}");
        }
    }
}
