using System;
using System.Globalization;
using System.Windows.Data;
using VRageMath;

namespace Torch.Server.Views.Converters
{
    public class Vector3DConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Vector3D)value).ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Vector3D.TryParse((string)value, out Vector3D vec))
                return vec;

            throw new ArgumentException();
        }
    }
}
