using API.Identity;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace API.Services.Implementations
{
    /// <summary>
    /// Creates JWT Authentication tokens for users when logged in. 
    /// Token contains user's details and roles, aswell adn is signed and has an expiry time
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenService"/> class with the specified JWT settings.
        /// </summary>
        /// <param name="options"></param>
        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a JWT token for the given user with the specified roles and optional extra claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roles"></param>
        /// <param name="extraClaims"></param>
        /// <returns></returns>
        public (string Token, DateTime ExpiresAtUtc) CreateToken(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<Claim>? extraClaims = null)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(r => new Claim("role", r)));

            if (extraClaims is not null)
            {
                claims.AddRange(extraClaims);
            }

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_settings.ExpiryMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                NotBefore = now,
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
            };

            return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
