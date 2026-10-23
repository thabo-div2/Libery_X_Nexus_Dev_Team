using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// Checks a password against the same rules the API enforces
    /// (12+ characters, at least one upper case letter, one lower case letter and one digit)
    /// so the user sees the problem before the form is sent.
    /// </summary>
    public sealed class StrongPasswordAttribute : ValidationAttribute
    {
        public const int MinimumLength = 12;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var password = value as string;

            // Leave empty values to [Required] so the user only sees one message.
            if (string.IsNullOrEmpty(password))
            {
                return ValidationResult.Success;
            }

            var members = new[] { validationContext.MemberName ?? string.Empty };

            if (password.Length < MinimumLength)
                return new ValidationResult($"Use at least {MinimumLength} characters.", members);

            if (!password.Any(char.IsUpper))
                return new ValidationResult("Add at least one capital letter.", members);

            if (!password.Any(char.IsLower))
                return new ValidationResult("Add at least one lower case letter.", members);

            if (!password.Any(char.IsDigit))
                return new ValidationResult("Add at least one number.", members);

            return ValidationResult.Success;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
