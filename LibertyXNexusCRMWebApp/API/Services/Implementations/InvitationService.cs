using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Models.Enums;
using System.Security.Claims;
using System.Security.Cryptography;

namespace API.Services.Implementations
{
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _invitations;
        private readonly IAdvisorRepository _advisors;

        public InvitationService(IInvitationRepository invitations, IAdvisorRepository advisors)
        {
            _invitations = invitations;
            _advisors = advisors;
        }

        public async Task<InvitationResult> CreateInvitation(CreateInvitationRequest request, int advisorId) 
        {
            var advisor = await _advisors.GetByIdAsync(advisorId);
            if (advisor is null)
                return new InvitationResult(
                        false,
                        "Invitation failed",
                        "",
                        null
                    );

            var invitation = new Invitation
            {
                AdvisorId = advisorId,
                Email = request.Email.Trim(),
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14)
            };

            await _invitations.AddAsync(invitation);

            return new InvitationResult(
                true,
                "Invitation created",
                invitation.Token,
                invitation.ExpiresAt);
        }

        public async Task<InvitationDetails> Validate(string token) 
        {
            var invitation = await _invitations.GetValidByTokenAsync(token);

            if (invitation is null)
            {
                return new InvitationDetails(
                        false,
                        "Invalid invitaion",
                        "",
                        0,
                        ""
                    );
            }

            var advisor = await _advisors.GetByIdAsync(invitation.AdvisorId);

            return new InvitationDetails(
                true,
                "Valid invitation",
                invitation.Email,
                invitation.AdvisorId,
                advisor?.FullName ?? string.Empty);
        }

        public sealed record CreateInvitationRequest(string Email);
        public sealed record InvitationResult(bool Success, string Message, string? Token, DateTime? ExpiresAt);
        public sealed record InvitationDetails(bool Valid, string Message, string Email, int AdvisorId, string AdvisorName);
    }
}
