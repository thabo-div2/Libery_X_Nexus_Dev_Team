using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// First screen of the forgot password page: the email to send the reset link to.
    /// </summary>
    public sealed class ForgotPasswordForm
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address, like name@example.com.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
