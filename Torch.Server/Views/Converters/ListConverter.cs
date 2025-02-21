using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Torch.Server.Views.Converters
{
    public class ListConverter : IValueConverter
    {
        public Type Type { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IList list))
                throw new InvalidOperationException("Value is not the proper type.");

            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Type));
            var converter = TypeDescriptor.GetConverter(Type);
            var input = ((string)value).Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in input)
            {
                try
                {
                    list.Add(converter.ConvertFromString(item));
                }
                catch
                {
                    throw new InvalidOperationException("Could not convert input value.");
                }
            }

            return list;
        }
    }
}
