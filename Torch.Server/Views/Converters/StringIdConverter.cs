using System;
using System.Globalization;
using System.Windows.Data;
using VRage.Utils;

namespace Torch.Server.Views.Converters
{
    public class StringIdConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MyStringId.GetOrCompute((string)value);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}