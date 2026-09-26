using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth
{
    /// <summary>
    /// Completes registration for a client who was invited by an advisor
    /// </summary>
    public class RegisterRequest
    {
        [Required, MaxLength(128)]
        public string InvitationToken { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30), Phone]
        public string? Phone { get; set; }

        [Required, MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
