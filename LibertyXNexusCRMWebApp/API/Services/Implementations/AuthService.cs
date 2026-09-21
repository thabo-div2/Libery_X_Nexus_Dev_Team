using API.DTOs.Auth;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

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

        public AuthService(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

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
            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, roles);

            return new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

        }
    }
}
