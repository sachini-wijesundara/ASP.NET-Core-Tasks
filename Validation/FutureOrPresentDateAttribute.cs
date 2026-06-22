using System;
using System.ComponentModel.DataAnnotations;

namespace ASP.NET_Core_Tasks.Validation
{
    public class FutureOrPresentDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime.Date < DateTime.Today)
                {
                    return new ValidationResult("The DueDate must be today or in the future.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
