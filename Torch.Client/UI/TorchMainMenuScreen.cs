#pragma warning disable 618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using SpaceEngineers.Game.GUI;
using Torch.Utils;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Torch.Client.UI
{
    public class TorchMainMenuScreen : MyGuiScreenMainMenu
    {
#pragma warning disable 169
        [ReflectedGetter(Name = "m_elementGroup")]
        private static Func<MyGuiScreenMainMenu, MyGuiControlElementGroup> _elementsGroup;
#pragma warning restore 169

        public TorchMainMenuScreen() : this(false)
        {
        }

        public TorchMainMenuScreen(bool pauseGame)
      : base(pauseGame)
        {
        }
        /// <inheritdoc />
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
            Vector2 value = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM, 54, 54) + new Vector2(minSizeGui.X / 2f, 0f) + new Vector2(15f, 0f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            
            MyGuiControlButton myGuiControlButton = MakeButton(value - 9 * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                MyStringId.GetOrCompute("Torch"), TorchButtonClicked, MyCommonTexts.ToolTipExitToWindows);
            Controls.Add(myGuiControlButton);
            _elementsGroup.Invoke(this).Add(myGuiControlButton);
        }

        private void TorchButtonClicked(MyGuiControlButton obj)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<TorchNavScreen>());
        }
    }
}
