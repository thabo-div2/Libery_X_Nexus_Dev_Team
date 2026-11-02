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

        /// <summary>
        /// Same as above, but a "Keep me signed in" token lasts days instead of an hour.
        /// </summary>
        (string Token, DateTime ExpiresAtUtc) CreateToken(
            ApplicationUser user,
            IEnumerable<string> roles,
            IEnumerable<Claim>? extraClaims,
            bool rememberMe);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
