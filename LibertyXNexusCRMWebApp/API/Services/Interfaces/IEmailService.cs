namespace API.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendInvitationAsync(
            string recipientEmail,
            string advisorName,
            string invitationLink,
            DateTime expiresAt);
    }
}
