using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace Torch.Client
{
    public class TorchSettingsScreen : MyGuiScreenBase
    {
        public override string GetFriendlyName() => "Piston Settings";

        public TorchSettingsScreen() : base(new Vector2(0.5f), null, new Vector2(0.5f), true, null, 0f, 0f)
        {
            this.BackgroundColor = new Vector4(0);
            RecreateControls(true);
        }

        public sealed override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(MyStringId.GetOrCompute("Piston Settings"));
        }
    }
}
