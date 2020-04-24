using System.Windows.Media;
using VRage.Game;
using Color = VRageMath.Color;

namespace Torch.Utils
{
    public static class ColorUtils
    {
        /// <summary>
        /// Convert the old "font" or a RGB hex code to a Color.
        /// </summary>
        public static Color TranslateColor(string font)
        {
            if (StringUtils.IsFontEnum(font))
            {
                // RGB values copied from Fonts.sbc
                switch (font)
                {
                    case MyFontEnum.Blue:
                        return new Color(220, 244, 252);
                    case MyFontEnum.Red:
                        return new Color(227, 65, 65);
                    case MyFontEnum.Green:
                        return new Color(101, 182, 193);
                    case MyFontEnum.DarkBlue:
                        return new Color(94, 115, 127);
                    default:
                        return Color.White;
                }
            }
            else
            {
                // VRage color doesn't have its own hex code parser and I don't want to write one
                var conv = (System.Windows.Media.Color)(ColorConverter.ConvertFromString(font) ?? 
                                                        System.Windows.Media.Color.FromRgb(255, 255, 255));
                return new Color(conv.R, conv.G, conv.B);
            }
        }
    }
}