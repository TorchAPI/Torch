using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

namespace Torch.Server.Views.ValidationRules
{
    public class ListConverterValidationRule : ValidationRule
    {
        public Type Type { get; set; }

        /// <inheritdoc />
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var converter = TypeDescriptor.GetConverter(Type);
            var input = ((string)value).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in input)
            {
                try
                {
                    converter.ConvertFromString(item);
                }
                catch
                {
                    return new ValidationResult(false, $"{item} is not a valid value.");
                }
            }

            return ValidationResult.ValidResult;
        }
    }
}