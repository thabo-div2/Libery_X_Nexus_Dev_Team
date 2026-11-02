using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        // "Keep me signed in" ticked: the login lasts days instead of an hour.
        public bool RememberMe { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
