using System;
using System.Globalization;
using System.Windows.Controls;

namespace PhotoApp.Validation
{
    internal class DateStartValidationRule : ValidationRule
    {
        public DateRangeChecker RangeChecker { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null)
            {
                return new ValidationResult(false, "Select date");
            }

            var startDate = (DateTime)value;
            var endDate = RangeChecker.EndDate;
            if (endDate < startDate)
            {
                return new ValidationResult(false, "");
            }

            return ValidationResult.ValidResult;
        }
    }
}
