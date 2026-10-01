using API.DTOs.Invitations;
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
        private readonly IInvitationRepository _invitationRepository;
        private readonly IAdvisorRepository _advisorRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public InvitationService(
            IInvitationRepository invitationRepository,
            IAdvisorRepository advisorRepository,
            IAuditLogRepository auditLogRepository)
        {
            _invitationRepository = invitationRepository;
            _advisorRepository = advisorRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<InvitationResponse> CreateAsync(int advisorId, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new InvitationResponse { Success = false, Message = "An email address is required." };
            }

            var invitation = new Invitation
            {
                AdvisorId = advisorId,
                Email = email.Trim(),
                Token = GenerateToken(),
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14)
            };

            await _invitationRepository.AddAsync(invitation);

            await _auditLogRepository.LogAsync(
                userId: advisorId,
                userRole: UserRole.FinancialAdviser,
                actionType: AuditActionType.Create,
                entityAffected: "Invitation",
                details: $"Advisor invited {invitation.Email}.");

            return new InvitationResponse
            {
                Success = true,
                Message = "Invitation created.",
                Token = invitation.Token,
                ExpiresAt = invitation.ExpiresAt
            };
        }

        public async Task<InvitationValidationResponse> ValidateAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new InvitationValidationResponse { Valid = false, Message = "No invitation token was provided." };
            }

            var invitation = await _invitationRepository.GetValidByTokenAsync(token);
            if (invitation is null)
            {
                return new InvitationValidationResponse
                {
                    Valid = false,
                    Message = "This invitation link is invalid, expired, or has already been used."
                };
            }

            var advisor = await _advisorRepository.GetByIdAsync(invitation.AdvisorId);

            return new InvitationValidationResponse
            {
                Valid = true,
                Message = "Invitation is valid.",
                Email = invitation.Email,
                AdvisorId = invitation.AdvisorId,
                AdvisorName = advisor?.FullName ?? string.Empty
            };
        }

        /// <summary>
        /// URL-safe, cryptographically random token. 32 bytes (256 bits) is
        /// comfortably enough entropy that guessing a valid token is
        /// infeasible — this token is effectively a bearer credential for
        /// registration, so it needs the same strength as a session token.
        /// </summary>
        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
