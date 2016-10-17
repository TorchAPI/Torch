using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using VRage.Utils;
using VRageMath;

namespace Piston.Client
{
    public class PistonConsoleScreen : MyGuiScreenBase
    {
        private MyGuiControlTextbox _textBox;

        public override string GetFriendlyName()
        {
            return "Piston Console";
        }

        public PistonConsoleScreen() : base(isTopMostScreen: true)
        {
            BackgroundColor = new Vector4(0, 0, 0, 0.5f);
            Size = new Vector2(0.5f);
            RecreateControls(true);
        }

        public sealed override void RecreateControls(bool constructor)
        {
            Elements.Clear();
            Elements.Add(new MyGuiControlLabel
            {
                Text = "Piston Console",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
            });

            Controls.Clear();
            _textBox = new MyGuiControlTextbox
            {
                BorderEnabled = false,
                Enabled = true,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(-0.5f)
            };
            Controls.Add(_textBox);

            var pistonBtn = new MyGuiControlImageButton
            {
                Name = "PistonButton",
                Text = "Piston",
                HighlightType = MyGuiControlHighlightType.WHEN_CURSOR_OVER,
                Visible = true,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            Controls.Add(pistonBtn);
        }
    }
}
