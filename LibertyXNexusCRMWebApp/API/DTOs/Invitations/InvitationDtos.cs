namespace API.DTOs.Invitations
{
    public class CreateInvitationRequest
    {
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
