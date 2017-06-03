using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Torch.Server.Views.Converters
{
    public class StringBuilderConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((StringBuilder)value).ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new StringBuilder((string)value);
        }
    }
}