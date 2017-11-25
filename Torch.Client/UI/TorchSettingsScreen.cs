using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Torch.Client.UI
{
    public class TorchSettingsScreen : MyGuiScreenBase
    {
        public TorchSettingsScreen() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR,
            new Vector2(0.35875f, 0.558333337f))
		{
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        /// <inheritdoc />
        public override string GetFriendlyName() => "Torch Settings";

        public void OnBackClick(MyGuiControlButton sender) => CloseScreen();
    }
}
