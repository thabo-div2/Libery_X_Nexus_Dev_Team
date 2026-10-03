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
    /// <summary>
    /// Service responsible for managing client invitations, including creation, validation, and email notifications.
    /// </summary>
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _invitationRepository;
        private readonly IAdvisorRepository _advisorRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvitationService> _logger;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationService"/> class with the specified dependencies.
        /// </summary>
        /// <param name="invitationRepository"></param>
        /// <param name="advisorRepository"></param>
        /// <param name="auditLogRepository"></param>
        /// <param name="emailService"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public InvitationService(
            IInvitationRepository invitationRepository,
            IAdvisorRepository advisorRepository,
            IAuditLogRepository auditLogRepository,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<InvitationService> logger)
        {
            _invitationRepository = invitationRepository;
            _advisorRepository = advisorRepository;
            _auditLogRepository = auditLogRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new invitation for a client to register with the system. Generates a unique token, saves the invitation, and sends an email to the client with the registration link.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<InvitationResponse> CreateAsync(int advisorId, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new InvitationResponse { Success = false, Message = "An email address is required." };
            }

            email = email.Trim();

            var advisor = await _advisorRepository.GetByIdAsync(advisorId);
            if (advisor is null)
            {
                return new InvitationResponse { Success = false, Message = "The advisor could not be found." };
            }

            var invitation = new Invitation
            {
                AdvisorId = advisorId,
                Email = email,
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

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                return new InvitationResponse
                {
                    Success = true,
                    Message = "Invitation created, but the frontend URL is not configured. The registration link could not be emailed.",
                    Token = invitation.Token,
                    ExpiresAt = invitation.ExpiresAt
                };
            }

            var invitationLink = $"{frontendBaseUrl.TrimEnd('/')}/register?token={Uri.EscapeDataString(invitation.Token)}";

            try
            {
                await _emailService.SendInvitationAsync(
                    invitation.Email,
                    advisor.FullName,
                    invitationLink,
                    invitation.ExpiresAt);
            }
            catch (Exception ex)
            {
                // Keep the invitation valid so the advisor can still copy/use the
                // generated link if email delivery is temporarily unavailable.
                _logger.LogError(
                    ex,
                    "Invitation {InvitationId} was created but could not be emailed to {Email}.",
                    invitation.InvitationId,
                    invitation.Email);

                return new InvitationResponse
                {
                    Success = true,
                    Message = "Invitation created, but the email could not be sent. You can use the generated registration link instead.",
                    Token = invitation.Token,
                    ExpiresAt = invitation.ExpiresAt
                };
            }

            return new InvitationResponse
            {
                Success = true,
                Message = "Invitation created and sent by email.",
                Token = invitation.Token,
                ExpiresAt = invitation.ExpiresAt
            };
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Validates an invitation token and returns the associated invitation details if valid.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Generates a secure random token for invitation links.
        /// </summary>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
