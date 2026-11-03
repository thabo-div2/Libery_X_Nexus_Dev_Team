using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Invitations
{
    /// <summary>
    /// Body of POST api/invitation/create. [ApiController] checks these rules before the
    /// action runs, so a missing or badly formed email returns 400 with a JSON error object
    /// instead of reaching the database. Same rules as the Invitation model's Email column.
    /// </summary>
    public class CreateInvitationRequest
    {
        [Required(ErrorMessage = "Enter the client's email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email can be at most 256 characters.")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Matches frontend.Services.InvitationResult exactly (Success,
    /// Message, Token, ExpiresAt) so no frontend remapping is needed.</summary>
    public class InvitationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Matches frontend.Services.InvitationDetails exactly (Valid,
    /// Message, Email, AdvisorId, AdvisorName).</summary>
    public class InvitationValidationResponse
    {
        public bool Valid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
