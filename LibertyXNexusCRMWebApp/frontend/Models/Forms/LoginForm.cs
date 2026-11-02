using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// Fields on the login page.
    /// </summary>
    public sealed class LoginForm
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address, like name@example.com.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        /// <summary>"Keep me signed in" - stay logged in after closing the browser.</summary>
        public bool RememberMe { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
