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
    [Plugin("Test Plugin", "1.3.3.7", "fed85d8d-8a29-4ab0-9869-4ad121f99d04")]
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
        public override void Dispose()
        {
            //Torch.Log.Write($"Plugin unload {Name}");
        }
    }
}
