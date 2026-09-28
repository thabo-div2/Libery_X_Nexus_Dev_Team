using System.Security.Cryptography;
using System.Text.Json;
using Shared.Models;
using Shared.Models.Enums;

namespace Backend.Services
{
    public record CreateInvitationRequest(string Email);
    public record InvitationResult(bool Success, string Message, string? Token, DateTime? ExpiresAt);
    public record InvitationDetails(bool Valid, string Message, string Email, int AdvisorId, string AdvisorName);

    public class InvitationService
    {
        private const int RatulAdvisorId = 1;
        private readonly string _invitationsPath;
        private readonly string _advisorsPath;

        public InvitationService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _invitationsPath = Path.Combine(dataDirectory, "invitations.json");
            _advisorsPath = Path.Combine(dataDirectory, "advisors.json");
        }

        public async Task<InvitationResult> CreateAsync(CreateInvitationRequest request)
        {
            var invitations = await ReadAsync<Invitation>(_invitationsPath);

            var invitation = new Invitation
            {
                InvitationId = invitations.Count == 0 ? 1 : invitations.Max(i => i.InvitationId) + 1,
                AdvisorId = RatulAdvisorId,
                Email = request.Email,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                Status = InvitationStatus.Pending
            };

            invitations.Add(invitation);
            await WriteAsync(_invitationsPath, invitations);

            return new InvitationResult(true, "Invitation created", invitation.Token, invitation.ExpiresAt);
        }

        public async Task<InvitationDetails> ValidateAsync(string token)
        {
            var invitations = await ReadAsync<Invitation>(_invitationsPath);
            var invitation = invitations.FirstOrDefault(i => i.Token == token);

            if (invitation is null || !invitation.IsValid)
            {
                return new InvitationDetails(false, "This invite link is invalid or has expired", string.Empty, 0, string.Empty);
            }

            var advisors = await ReadAsync<Advisor>(_advisorsPath);
            var advisor = advisors.FirstOrDefault(a => a.AdvisorId == invitation.AdvisorId);

            return new InvitationDetails(true, "Valid invitation", invitation.Email, invitation.AdvisorId, advisor?.FullName ?? string.Empty);
        }

        public async Task<bool> RedeemAsync(string token, int clientId)
        {
            var invitations = await ReadAsync<Invitation>(_invitationsPath);
            var invitation = invitations.FirstOrDefault(i => i.Token == token);

            if (invitation is null || !invitation.IsValid)
            {
                return false;
            }

            invitation.Status = InvitationStatus.Redeemed;
            invitation.RedeemedByIdClient = clientId;
            invitation.RedeemedAt = DateTime.UtcNow;

            await WriteAsync(_invitationsPath, invitations);
            return true;
        }

        private static async Task<List<T>> ReadAsync<T>(string path)
        {
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream);
            return data ?? new List<T>();
        }

        private static async Task WriteAsync<T>(string path, List<T> data)
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
