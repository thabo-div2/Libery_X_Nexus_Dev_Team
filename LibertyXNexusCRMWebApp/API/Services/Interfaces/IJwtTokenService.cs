using API.Identity;
using System.Security.Claims;

namespace API.Services.Interfaces
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAtUtc) CreateToken(
            ApplicationUser user,
            IEnumerable<string> roles,
            IEnumerable<Claim>? extraClaims = null);
    }
}
