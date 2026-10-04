namespace API.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendInvitationAsync(
            string recipientEmail,
            string advisorName,
            string invitationLink,
            DateTime expiresAt);

        Task SendPasswordResetAsync(
            string recipientEmail,
            string resetLink);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
