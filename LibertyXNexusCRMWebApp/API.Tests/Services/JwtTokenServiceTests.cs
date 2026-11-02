using API.Identity;
using API.Services.Implementations;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Tests for how long login tokens last.
    /// </summary>
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _sut = new(Options.Create(new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "a-test-signing-key-that-is-at-least-32-bytes-long",
            ExpiryMinutes = 60,
            RememberMeExpiryDays = 7
        }));

        private static readonly ApplicationUser User = new() { Id = "user-1", Email = "user@nexus.test" };

        /// <summary>
        /// A normal login lasts the normal number of minutes.
        /// </summary>
        [Fact]
        public void CreateToken_Normally_LastsAnHour()
        {
            var (token, expires) = _sut.CreateToken(User, new[] { AppRoles.Client });

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.InRange(expires, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        }

        /// <summary>
        /// "Keep me signed in" lasts days instead of an hour.
        /// </summary>
        [Fact]
        public void CreateToken_WithRememberMe_LastsSevenDays()
        {
            var (token, expires) = _sut.CreateToken(User, new[] { AppRoles.Client }, null, rememberMe: true);

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.InRange(expires, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
