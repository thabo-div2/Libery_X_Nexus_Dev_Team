using API.DTOs.Auth;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Shared.Models.Enums;
using Shared.Models;
using System.Security.Claims;

namespace API.Services.Implementations
{
    /// <summary>
    /// Handles the user login process by checking user's account and password
    /// If Successful it will create Jwt token with user's authentication details and role
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly IAdvisorRepository _advisorRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IInvitationRepository _invitationRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            ILogger<AuthService> logger,
            IAdvisorRepository advisorRepository,
            IClientRepository clientRepository,
            IInvitationRepository invitationRepository,
            IAuditLogRepository auditLogRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
            _advisorRepository = advisorRepository;
            _clientRepository = clientRepository;
            _invitationRepository = invitationRepository;
            _auditLogRepository = auditLogRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _configuration = configuration;
        }

        /// <summary>
        /// Logs the user in and gives back a token.
        /// </summary>
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || !user.IsActive)
            {
                _logger.LogWarning("Failed Login: Unknown or Inactive account");
                return null;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Failed Login: Account {UserId} is locked out", user.Id);
                return null;
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                await _userManager.AccessFailedAsync(user);
                _logger.LogWarning("Failed Login: Wrong Password for {UserId}", user.Id);
                return null;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            /// <summary>
            /// Identifies if logged-in user is an advisor or client and adds their AdvisorId or ClientId to the token
            ///</summary>
            var extraClaims = new List<Claim>();
            int? domainUserId = null;
            UserRole? userRole = null;

            if (roles.Contains(AppRoles.Advisor))
            {
                var advisor = await _advisorRepository.GetByIdentitySubjectIdAsync(user.Id);

                if (advisor is not null)
                {
                    extraClaims.Add(new Claim("advisorId", advisor.AdvisorId.ToString()));
                    extraClaims.Add(new Claim("firstName", advisor.FirstName ?? string.Empty));
                    extraClaims.Add(new Claim("lastName", advisor.LastName ?? string.Empty));
                    domainUserId = advisor.AdvisorId;
                }
                userRole = UserRole.FinancialAdviser;
            }
            else if (roles.Contains(AppRoles.Client) && user.Email is not null)
            {
                var client = await _clientRepository.GetByEmailAsync(user.Email);

                if (client is not null)
                {
                    extraClaims.Add(new Claim("clientId", client.ClientId.ToString()));
                    extraClaims.Add(new Claim("firstName", client.FirstName ?? string.Empty));
                    extraClaims.Add(new Claim("lastName", client.LastName ?? string.Empty));
                    domainUserId = client.ClientId;
                }
                userRole = UserRole.RegisteredClient;
            }

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, roles, extraClaims);

            await _auditLogRepository.LogAsync(
                userId: domainUserId,
                userRole: userRole ?? UserRole.ProspectiveClient,
                actionType: AuditActionType.Login,
                entityAffected: "Auth",
                details: $"Successful login for {user.Email}");


            return new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

        }

        /// <summary>
        /// Registers a client from an invite.
        /// </summary>
        public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
        {
            var invitation = await _invitationRepository.GetValidByTokenAsync(request.InvitationToken);

            if (invitation is null)
            {
                return new RegisterResult
                {
                    Success = false,
                    Error = "This invitation link is invalid, expired, or has already been used"
                };
            }

            var existingUser = await _userManager.FindByEmailAsync(invitation.Email);

            if (existingUser is not null)
            {
                return new RegisterResult
                {
                    Success = false,
                    Error = "An account with this email already exists"
                };

            }

            var user = new ApplicationUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                EmailConfirmed = true
            };

            var created = await _userManager.CreateAsync(user, request.Password);

            if (!created.Succeeded)
            {
                return new RegisterResult
                {
                    Success = false,
                    Error = string.Join("; ", created.Errors.Select(e => e.Description))
                };
            }

            await _userManager.AddToRoleAsync(user, AppRoles.Client);

            var client = new Client
            {
                IdentityProviderSubjectId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = invitation.Email,
                Phone = request.Phone,
                AdvisorId = invitation.AdvisorId,
                Status = ClientStatus.Registered,
                CreatedAt = DateTime.UtcNow
            };

            var createdClient = await _clientRepository.AddAsync(client);

            await _invitationRepository.MarkRedeemedAsync(invitation.InvitationId, createdClient.ClientId);

            await _auditLogRepository.LogAsync(
                userId: createdClient.ClientId,
                userRole: UserRole.RegisteredClient,
                actionType: AuditActionType.Create,
                entityAffected: "Client",
                details: $"Client self-registered from a invitation {invitation.InvitationId}");

            await _notificationService.NotifyAdvisorAsync(
                invitation.AdvisorId,
                NotificationType.ClientRegistered,
                $"{createdClient.FirstName} {createdClient.LastName} has accepted the invitation.",
                "/clients",
                createdClient.ClientId);

            var extraClaims = new[]
            {
                new Claim("clientId", createdClient.ClientId.ToString())
            };

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, new[] { AppRoles.Client }, extraClaims);

            return new RegisterResult
            {
                Success = true,
                Response = new AuthResponse
                {
                    AccessToken = token,
                    ExpiresAtUtc = expiresAtUtc,
                    Email = user.Email ?? string.Empty,
                    Role = AppRoles.Client
                }
            };
        }

        /// <summary>
        /// Emails the user a reset link.
        /// </summary>
        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            const string genericMessage =
                "If an account exists for that email address, " +
                "a password reset link has been sent.";

            var normalizedEmail = request.Email.Trim();

            var user = await _userManager.FindByEmailAsync(normalizedEmail);

            // Do not reveal whether an account exists.
            if (user is null || !user.IsActive)
            {
                _logger.LogInformation(
                    "Password reset requested for an unknown or inactive account.");

                return new ForgotPasswordResponse
                {
                    Message = genericMessage
                };
            }

            var resetToken =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"];

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                _logger.LogError(
                    "Frontend:BaseUrl is not configured.");

                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured.");
            }

            var resetLink =
                $"{frontendBaseUrl.TrimEnd('/')}/forgot-password" +
                $"?email={Uri.EscapeDataString(normalizedEmail)}" +
                $"&token={Uri.EscapeDataString(resetToken)}";

            await _emailService.SendPasswordResetAsync(
                normalizedEmail,
                resetLink);

            _logger.LogInformation(
                "Password reset email generated for user {UserId}.",
                user.Id);

            return new ForgotPasswordResponse
            {
                Message = genericMessage
            };
        }

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || !user.IsActive)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Error = "No account was found for that email."
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);

            if (!result.Succeeded)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Error = string.Join("; ", result.Errors.Select(e => e.Description))
                };
            }

            // A successful reset clears any lockout, same as a successful login would.
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("Password reset completed for {UserId}", user.Id);

            return new ResetPasswordResult { Success = true };
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
