using API.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace API.Tests.Security
{
    /// <summary>
    /// Tests for the API's rate limiting: the settings, who a request is counted against,
    /// the limiter itself, and the 429 response.
    /// </summary>
    public class RateLimitingTests
    {
        private static HttpContext AnonymousRequestFrom(string ipAddress)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
            return context;
        }

        private static HttpContext SignedInRequestFrom(string ipAddress, string userId)
        {
            var context = AnonymousRequestFrom(ipAddress);
            var identity = new ClaimsIdentity(new[] { new Claim("sub", userId) }, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
            return context;
        }

        private static RateLimiterOptions BuildLimiterOptions(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = new ServiceCollection();
            services.AddApiRateLimiting(configuration);

            return services.BuildServiceProvider().GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        }

        // ------------------------------------------------------------------ settings

        /// <summary>
        /// The default settings are valid.
        /// </summary>
        [Fact]
        public void Validate_WithDefaultSettings_DoesNotThrow()
        {
            var options = new RateLimitingOptions();

            var exception = Record.Exception(options.Validate);

            Assert.Null(exception);
        }

        /// <summary>
        /// A zero or negative limit stops the API at startup instead of silently blocking every request.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Validate_WhenALimitIsNotPositive_Throws(int badValue)
        {
            var options = new RateLimitingOptions { AuthRequestsPerWindow = badValue };

            var exception = Assert.Throws<InvalidOperationException>(options.Validate);

            Assert.Contains(nameof(RateLimitingOptions.AuthRequestsPerWindow), exception.Message);
        }

        /// <summary>
        /// A bad value in the configuration is caught when rate limiting is added.
        /// </summary>
        [Fact]
        public void AddApiRateLimiting_WhenConfigurationHasABadValue_Throws()
        {
            var settings = new Dictionary<string, string?> { ["RateLimiting:WindowSeconds"] = "0" };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApiRateLimiting(configuration));
        }

        // ------------------------------------------------------------------ who a request counts against

        /// <summary>
        /// A signed-in user is counted by their own id, whatever address they come from.
        /// </summary>
        [Fact]
        public void GetPartitionKey_WhenSignedIn_UsesTheUserId()
        {
            var context = SignedInRequestFrom("10.0.0.1", "user-42");

            Assert.Equal("user:user-42", RateLimitingExtensions.GetPartitionKey(context));
        }

        /// <summary>
        /// Someone who is not signed in is counted by address.
        /// </summary>
        [Fact]
        public void GetPartitionKey_WhenNotSignedIn_UsesTheAddress()
        {
            var context = AnonymousRequestFrom("10.0.0.7");

            Assert.Equal("ip:10.0.0.7", RateLimitingExtensions.GetPartitionKey(context));
        }

        /// <summary>
        /// A signed-in principal without an id cannot be told apart from others, so it falls back to the address.
        /// </summary>
        [Fact]
        public void GetPartitionKey_WhenSignedInWithoutAnId_FallsBackToTheAddress()
        {
            var context = AnonymousRequestFrom("10.0.0.9");
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("email", "a@b.test") }, "TestAuth"));

            Assert.Equal("ip:10.0.0.9", RateLimitingExtensions.GetPartitionKey(context));
        }

        /// <summary>
        /// A request with no known address still gets a key instead of failing.
        /// </summary>
        [Fact]
        public void GetClientIpKey_WhenAddressIsUnknown_UsesAPlaceholder()
        {
            var context = new DefaultHttpContext();

            Assert.Equal("ip:unknown", RateLimitingExtensions.GetClientIpKey(context));
        }

        // ------------------------------------------------------------------ the limiter

        /// <summary>
        /// An address that goes over its limit is refused, while a different address is not affected.
        /// </summary>
        [Fact]
        public void GlobalLimiter_WhenAnAnonymousAddressGoesOverItsLimit_RefusesItButNotOthers()
        {
            var options = BuildLimiterOptions(new() { ["RateLimiting:AnonymousRequestsPerWindow"] = "2" });
            using var limiter = options.GlobalLimiter!;

            using var first = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
            using var second = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
            using var third = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
            using var otherAddress = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.2"));

            Assert.True(first.IsAcquired);
            Assert.True(second.IsAcquired);
            Assert.False(third.IsAcquired);
            Assert.True(otherAddress.IsAcquired);
        }

        /// <summary>
        /// Signed-in users have their own limit, so a busy anonymous address does not block them.
        /// </summary>
        [Fact]
        public void GlobalLimiter_WhenAnonymousAddressIsExhausted_StillAllowsASignedInUser()
        {
            var options = BuildLimiterOptions(new()
            {
                ["RateLimiting:AnonymousRequestsPerWindow"] = "1",
                ["RateLimiting:UserRequestsPerWindow"] = "3"
            });
            using var limiter = options.GlobalLimiter!;

            using var anonymousFirst = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
            using var anonymousSecond = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
            using var userFirst = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));
            using var userSecond = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));
            using var userThird = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));
            using var userFourth = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));

            Assert.True(anonymousFirst.IsAcquired);
            Assert.False(anonymousSecond.IsAcquired);
            Assert.True(userFirst.IsAcquired);
            Assert.True(userSecond.IsAcquired);
            Assert.True(userThird.IsAcquired);
            Assert.False(userFourth.IsAcquired);
        }

        /// <summary>
        /// Two signed-in users are counted separately.
        /// </summary>
        [Fact]
        public void GlobalLimiter_WhenOneUserIsOverTheirLimit_AnotherUserIsNotAffected()
        {
            var options = BuildLimiterOptions(new() { ["RateLimiting:UserRequestsPerWindow"] = "1" });
            using var limiter = options.GlobalLimiter!;

            using var userOneFirst = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));
            using var userOneSecond = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-1"));
            using var userTwo = limiter.AttemptAcquire(SignedInRequestFrom("10.0.0.1", "user-2"));

            Assert.True(userOneFirst.IsAcquired);
            Assert.False(userOneSecond.IsAcquired);
            Assert.True(userTwo.IsAcquired);
        }

        /// <summary>
        /// Rate limiting can be switched off with the Enabled setting.
        /// </summary>
        [Fact]
        public void GlobalLimiter_WhenDisabled_NeverRefusesARequest()
        {
            var options = BuildLimiterOptions(new()
            {
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:AnonymousRequestsPerWindow"] = "1"
            });
            using var limiter = options.GlobalLimiter!;

            for (var i = 0; i < 10; i++)
            {
                using var lease = limiter.AttemptAcquire(AnonymousRequestFrom("10.0.0.1"));
                Assert.True(lease.IsAcquired);
            }
        }

        /// <summary>
        /// Refused requests get a 429.
        /// </summary>
        [Fact]
        public void RateLimiting_UsesStatus429ForRefusedRequests()
        {
            var options = BuildLimiterOptions(new());

            Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
            Assert.NotNull(options.OnRejected);
        }

        // ------------------------------------------------------------------ the 429 response

        /// <summary>
        /// The 429 body is a JSON error object and the Retry-After header is rounded up to whole seconds.
        /// </summary>
        [Fact]
        public async Task WriteTooManyRequestsAsync_WithARetryTime_WritesJsonErrorAndRetryAfterHeader()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await RateLimitingExtensions.WriteTooManyRequestsAsync(context, TimeSpan.FromSeconds(29.2));

            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            Assert.Equal("30", context.Response.Headers.RetryAfter.ToString());

            context.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("message").GetString()));
            Assert.Equal(30, json.RootElement.GetProperty("retryAfterSeconds").GetInt32());
        }

        /// <summary>
        /// When the wait time is not known, there is no Retry-After header but the body is still a JSON error object.
        /// </summary>
        [Fact]
        public async Task WriteTooManyRequestsAsync_WithoutARetryTime_OmitsTheHeaderButStillWritesAnError()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await RateLimitingExtensions.WriteTooManyRequestsAsync(context, null);

            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey("Retry-After"));

            context.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("message").GetString()));
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("retryAfterSeconds").ValueKind);
        }
    }
}