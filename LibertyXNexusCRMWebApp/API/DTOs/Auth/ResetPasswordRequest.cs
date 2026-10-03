using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string ResetToken { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
