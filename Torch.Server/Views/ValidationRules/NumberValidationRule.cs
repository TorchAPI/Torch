using System.Globalization;
using System.Windows.Controls;

namespace Torch.Server.Views.ValidationRules
{
    public class NumberValidationRule : ValidationRule
    {
        /// <inheritdoc />
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!float.TryParse(value?.ToString(), out _))
                return new ValidationResult(false, "Not a number.");

            return ValidationResult.ValidResult;
        }
    }
}
