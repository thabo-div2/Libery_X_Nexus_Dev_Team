using API.DTOs.Auth;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IJwtTokenService> _jwtTokenService;
        private readonly Mock<ILogger<AuthService>> _logger;
        private readonly Mock<IAdvisorRepository> _advisorRepository;
        private readonly Mock<IClientRepository> _clientRepository;
        private readonly Mock<IInvitationRepository> _invitationRepository;
        private readonly Mock<IAuditLogRepository> _auditLogRepository;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userManager = MockUserManagerHelper.Create();
            _jwtTokenService = new Mock<IJwtTokenService>();
            _logger = new Mock<ILogger<AuthService>>();
            _advisorRepository = new Mock<IAdvisorRepository>();
            _clientRepository = new Mock<IClientRepository>();
            _invitationRepository = new Mock<IInvitationRepository>();
            _auditLogRepository = new Mock<IAuditLogRepository>();

            _auditLogRepository
                .Setup(r => r.LogAsync(
                    It.IsAny<int?>(), It.IsAny<UserRole>(), It.IsAny<AuditActionType>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            _jwtTokenService
                .Setup(j => j.CreateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
                .Returns(("fake-jwt-token", DateTime.UtcNow.AddHours(1)));

            _sut = new AuthService(
                _userManager.Object,
                _jwtTokenService.Object,
                _logger.Object,
                _advisorRepository.Object,
                _clientRepository.Object,
                _invitationRepository.Object,
                _auditLogRepository.Object);
        }

        private static ApplicationUser MakeUser(string email, bool isActive = true) => new()
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            IsActive = isActive
        };

        [Fact]
        public async Task LoginAsync_WithUnknownEmail_ReturnsNull()
        {
            _userManager.Setup(m => m.FindByEmailAsync("missing@nexus.test")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.LoginAsync(new LoginRequest { Email = "missing@nexus.test", Password = "whatever" });

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveAccount_ReturnsNull()
        {
            var user = MakeUser("inactive@nexus.test", isActive: false);
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

            var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email!, Password = "whatever" });

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithLockedOutAccount_ReturnsNull()
        {
            var user = MakeUser("locked@nexus.test");
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email!, Password = "whatever" });

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ReturnsNullAndRecordsFailure()
        {
            var user = MakeUser("client@nexus.test");
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);
            _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email!, Password = "wrong-password" });

            Assert.Null(result);
            _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithValidClientCredentials_ReturnsTokenAndClientRole()
        {
            var user = MakeUser("client@nexus.test");
            var client = new Client { ClientId = 7, FirstName = "Jane", LastName = "Doe", Email = user.Email!, IdentityProviderSubjectId = user.Id };

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "correct-password")).ReturnsAsync(true);
            _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { AppRoles.Client });
            _clientRepository.Setup(r => r.GetByEmailAsync(user.Email!)).ReturnsAsync(client);

            var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email!, Password = "correct-password" });

            Assert.NotNull(result);
            Assert.Equal("fake-jwt-token", result!.AccessToken);
            Assert.Equal(AppRoles.Client, result.Role);
        }

        [Fact]
        public async Task RegisterAsync_WithInvalidInvitation_ReturnsFailure()
        {
            _invitationRepository.Setup(r => r.GetValidByTokenAsync("bad-token")).ReturnsAsync((Invitation?)null);

            var result = await _sut.RegisterAsync(new RegisterRequest
            {
                InvitationToken = "bad-token",
                FirstName = "Jane",
                LastName = "Doe",
                Password = "Password123"
            });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task RegisterAsync_WithAlreadyUsedEmail_ReturnsFailure()
        {
            var invitation = new Invitation { InvitationId = 1, AdvisorId = 1, Email = "taken@nexus.test", Token = "good-token" };
            _invitationRepository.Setup(r => r.GetValidByTokenAsync("good-token")).ReturnsAsync(invitation);
            _userManager.Setup(m => m.FindByEmailAsync(invitation.Email)).ReturnsAsync(MakeUser(invitation.Email));

            var result = await _sut.RegisterAsync(new RegisterRequest
            {
                InvitationToken = "good-token",
                FirstName = "Jane",
                LastName = "Doe",
                Password = "Password123"
            });

            Assert.False(result.Success);
            Assert.Contains("already exists", result.Error);
        }

        [Fact]
        public async Task RegisterAsync_WithValidInvitation_CreatesClientAndReturnsToken()
        {
            var invitation = new Invitation { InvitationId = 1, AdvisorId = 1, Email = "new@nexus.test", Token = "good-token" };
            _invitationRepository.Setup(r => r.GetValidByTokenAsync("good-token")).ReturnsAsync(invitation);
            _userManager.Setup(m => m.FindByEmailAsync(invitation.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123")).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.Client)).ReturnsAsync(IdentityResult.Success);

            var createdClient = new Client { ClientId = 42, FirstName = "Jane", LastName = "Doe", Email = invitation.Email };
            _clientRepository.Setup(r => r.AddAsync(It.IsAny<Client>())).ReturnsAsync(createdClient);
            _invitationRepository.Setup(r => r.MarkRedeemedAsync(invitation.InvitationId, createdClient.ClientId)).Returns(Task.CompletedTask);

            var result = await _sut.RegisterAsync(new RegisterRequest
            {
                InvitationToken = "good-token",
                FirstName = "Jane",
                LastName = "Doe",
                Password = "Password123"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Response);
            Assert.Equal("fake-jwt-token", result.Response!.AccessToken);
            Assert.Equal(AppRoles.Client, result.Response.Role);
            _invitationRepository.Verify(r => r.MarkRedeemedAsync(invitation.InvitationId, createdClient.ClientId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenIdentityCreationFails_ReturnsFailureWithDescriptions()
        {
            var invitation = new Invitation { InvitationId = 1, AdvisorId = 1, Email = "new@nexus.test", Token = "good-token" };
            _invitationRepository.Setup(r => r.GetValidByTokenAsync("good-token")).ReturnsAsync(invitation);
            _userManager.Setup(m => m.FindByEmailAsync(invitation.Email)).ReturnsAsync((ApplicationUser?)null);

            var failure = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "weak")).ReturnsAsync(failure);

            var result = await _sut.RegisterAsync(new RegisterRequest
            {
                InvitationToken = "good-token",
                FirstName = "Jane",
                LastName = "Doe",
                Password = "weak"
            });

            Assert.False(result.Success);
            Assert.Contains("Password too weak", result.Error);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithExistingActiveAccount_ReturnsExistsAndToken()
        {
            var user = MakeUser("client@nexus.test");
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-123");

            var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email! });

            Assert.True(result.Exists);
            Assert.Equal("reset-token-123", result.ResetToken);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithUnknownEmail_ReturnsDoesNotExist()
        {
            _userManager.Setup(m => m.FindByEmailAsync("missing@nexus.test")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "missing@nexus.test" });

            Assert.False(result.Exists);
            Assert.Null(result.ResetToken);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithInactiveAccount_ReturnsDoesNotExist()
        {
            var user = MakeUser("inactive@nexus.test", isActive: false);
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

            var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email! });

            Assert.False(result.Exists);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithValidTokenAndAccount_Succeeds()
        {
            var user = MakeUser("client@nexus.test");
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token-123", "NewPassword123")).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = user.Email!,
                ResetToken = "reset-token-123",
                NewPassword = "NewPassword123"
            });

            Assert.True(result.Success);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithUnknownEmail_Fails()
        {
            _userManager.Setup(m => m.FindByEmailAsync("missing@nexus.test")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "missing@nexus.test",
                ResetToken = "whatever",
                NewPassword = "NewPassword123"
            });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithInvalidToken_FailsWithIdentityError()
        {
            var user = MakeUser("client@nexus.test");
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

            var failure = IdentityResult.Failed(new IdentityError { Description = "Invalid token." });
            _userManager.Setup(m => m.ResetPasswordAsync(user, "bad-token", "NewPassword123")).ReturnsAsync(failure);

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = user.Email!,
                ResetToken = "bad-token",
                NewPassword = "NewPassword123"
            });

            Assert.False(result.Success);
            Assert.Contains("Invalid token.", result.Error);
        }
    }
}
