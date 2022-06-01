using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Torch.Client.UI
{
    public class TorchNavScreen : MyGuiScreenBase
    {
		private MyGuiControlElementGroup _elementGroup;

        public TorchNavScreen() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.35875f, 0.558333337f))
		{
			EnabledBackgroundFade = true;
			RecreateControls(true);
		}

		public override void RecreateControls(bool constructor)
		{
			base.RecreateControls(constructor);
			_elementGroup = new MyGuiControlElementGroup();
			_elementGroup.HighlightChanged += ElementGroupHighlightChanged;
			AddCaption(MyCommonTexts.ScreenCaptionOptions, null, null);
			var value = new Vector2(0f, -m_size.Value.Y / 2f + 0.146f);
			var num = 0;
			var myGuiControlButton = new MyGuiControlButton(value + num++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, MyTexts.Get(MyCommonTexts.ScreenOptionsButtonGame), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, delegate(MyGuiControlButton sender)
			{
				MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<TorchSettingsScreen>());
			}, GuiSounds.MouseClick, 1f, null);
			Controls.Add(myGuiControlButton);
			_elementGroup.Add(myGuiControlButton);
			CloseButtonEnabled = true;
		}

		private void ElementGroupHighlightChanged(MyGuiControlElementGroup obj)
		{
			foreach (MyGuiControlBase current in _elementGroup)
				if (current.HasFocus && obj.SelectedElement != current)
					FocusedControl = obj.SelectedElement;
		}

        public override string GetFriendlyName() => "Torch";

        public void OnBackClick(MyGuiControlButton sender) => CloseScreen();
    }
}
