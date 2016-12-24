using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using SpaceEngineers.Game.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Torch.Client
{
    public class TorchMainMenuScreen : MyGuiScreenMainMenu
    {
        /// <inheritdoc />
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            var buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
            Vector2 leftButtonPositionOrigin = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM) + new Vector2(buttonSize.X / 2f, 0f);
            var btn = MakeButton(leftButtonPositionOrigin - 9 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyStringId.GetOrCompute("Torch"), TorchButtonClicked);
            Controls.Add(btn);
        }

        private void TorchButtonClicked(MyGuiControlButton obj)
        {
            MyGuiSandbox.AddScreen(new TorchSettingsScreen());
        }
    }
}
