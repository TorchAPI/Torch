using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace Torch.Client
{
    public class TorchSettingsScreen : MyGuiScreenBase
    {
        public override string GetFriendlyName() => "Torch Settings";

        public TorchSettingsScreen() : base(new Vector2(0.5f), null, Vector2.One, true)
        {
            RecreateControls(true);
        }

        public sealed override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(MyStringId.GetOrCompute("Torch Settings"), null, new Vector2(0, 0), 1.2f);
            var pluginList = new MyGuiControlListbox
            {
                VisibleRowsCount = 10,
            };

            foreach (var plugin in TorchBase.Instance.Plugins.Plugins)
            {
                var name = TorchBase.Instance.Plugins.GetPluginName(plugin.GetType());
                pluginList.Items.Add(new MyGuiControlListbox.Item(new StringBuilder(name)));
            }
            Controls.Add(pluginList);
        }
    }
}
