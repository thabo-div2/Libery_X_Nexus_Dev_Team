using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// Fields on the register page (opened from an advisor's invite link).
    /// </summary>
    public sealed class RegisterForm
    {
        [Required(ErrorMessage = "Enter your first name.")]
        [MaxLength(100, ErrorMessage = "First name can be at most 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [MaxLength(100, ErrorMessage = "Last name can be at most 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address, like name@example.com.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        // Optional. [Phone] would reject an empty box, so a pattern is used instead (empty passes).
        [RegularExpression(@"^\+?[0-9 ()\-]{7,20}$", ErrorMessage = "Enter a valid phone number, like +27 82 000 0000.")]
        [MaxLength(30)]
        public string Mobile { get; set; } = string.Empty;

        [RegularExpression(@"^\d{13}$", ErrorMessage = "A South African ID number must be 13 digits.")]
        public string IdNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a password.")]
        [StrongPassword]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        // A checkbox can't be [Required] (false is still a value), so it must be exactly true.
        [Range(typeof(bool), "true", "true", ErrorMessage = "Tick the box to agree to POPIA processing before creating your account.")]
        public bool PopiaConsent { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
