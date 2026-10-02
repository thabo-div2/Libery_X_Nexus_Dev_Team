using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;

namespace API.Services.Implementations
{
    public sealed class SmtpEmailOptions
    {
        public const string SectionName = "Email:Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Liberty X Nexus";
        public bool EnableSsl { get; set; } = true;
    }

    public sealed class SmtpEmailService : IEmailService
    {
        private readonly SmtpEmailOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<SmtpEmailOptions> options,
            ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendInvitationAsync(
            string recipientEmail,
            string advisorName,
            string invitationLink,
            DateTime expiresAt)
        {
            ValidateConfiguration();

            var safeAdvisorName = HtmlEncoder.Default.Encode(
                string.IsNullOrWhiteSpace(advisorName) ? "your financial adviser" : advisorName);
            var safeLink = HtmlEncoder.Default.Encode(invitationLink);
            var expiryText = expiresAt.ToUniversalTime().ToString("dd MMM yyyy HH:mm 'UTC'");

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = "You're invited to Liberty X Nexus",
                IsBodyHtml = true,
                Body = $"""
                    <!DOCTYPE html>
                    <html>
                    <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #222;">
                        <h2>You're invited to Liberty X Nexus</h2>
                        <p>{safeAdvisorName} has invited you to create your client account.</p>
                        <p>
                            Click the button below to complete your registration:
                        </p>
                        <p>
                            <a href="{safeLink}"
                               style="display:inline-block;padding:12px 20px;background:#111;color:#fff;text-decoration:none;border-radius:6px;">
                                Complete registration
                            </a>
                        </p>
                        <p>If the button does not work, copy and paste this link into your browser:</p>
                        <p>{safeLink}</p>
                        <p>This invitation expires on {expiryText}.</p>
                        <p>If you were not expecting this invitation, you can ignore this email.</p>
                        <p>Regards,<br/>Liberty X Nexus</p>
                    </body>
                    </html>
                    """
            };

            message.To.Add(new MailAddress(recipientEmail));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {RecipientEmail}.", recipientEmail);
                throw;
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.Host) ||
                string.IsNullOrWhiteSpace(_options.Username) ||
                string.IsNullOrWhiteSpace(_options.Password) ||
                string.IsNullOrWhiteSpace(_options.FromEmail))
            {
                throw new InvalidOperationException(
                    "Email SMTP configuration is incomplete. Configure Email:Smtp:Host, Username, Password and FromEmail.");
            }
        }
    }
}
