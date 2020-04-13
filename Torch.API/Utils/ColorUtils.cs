using System.Windows.Media;
using VRage.Game;
using Color = VRageMath.Color;

namespace Torch.Utils
{
    public static class ColorUtils
    {
        /// <summary>
        /// Convert the old "font" to a Color.
        /// </summary>
        public static Color TranslateColor(string font)
        {
            if (StringUtils.IsFontEnum(font))
            {
                switch (font)
                {
                    case MyFontEnum.Blue:
                        return Color.Blue;
                    case MyFontEnum.Red:
                        return Color.Red;
                    case MyFontEnum.Green:
                        return Color.Green;
                    default:
                        return Color.White;
                }
            }
            else
            {
                var conv = (System.Windows.Media.Color)(ColorConverter.ConvertFromString(font) ?? 
                                                        System.Windows.Media.Color.FromRgb(255, 255, 255));
                return new Color(conv.R, conv.G, conv.B);
            }
        }
    }
}