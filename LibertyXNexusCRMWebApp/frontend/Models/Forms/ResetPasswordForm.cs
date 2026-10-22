using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// Last screen of the forgot password page: choosing the new password.
    /// </summary>
    public sealed class ResetPasswordForm
    {
        [Required(ErrorMessage = "Enter a new password.")]
        [StrongPassword]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type the new password again.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Those passwords don't match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
