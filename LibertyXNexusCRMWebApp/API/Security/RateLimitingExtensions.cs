using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace API.Security
{
    /// <summary>
    /// The names of the rate limit policies that can be put on a controller or action
    /// with <c>[EnableRateLimiting(...)]</c>.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string Auth = "auth";
        public const string PasswordReset = "password-reset";
        public const string Upload = "upload";
    }

    /// <summary>
    /// Sets up rate limiting for the API so one caller cannot flood it with requests.
    /// </summary>
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                ?? new RateLimitingOptions();
            settings.Validate();

            var window = TimeSpan.FromSeconds(settings.WindowSeconds);
            var passwordResetWindow = TimeSpan.FromMinutes(settings.PasswordResetWindowMinutes);
            var uploadWindow = TimeSpan.FromMinutes(settings.UploadWindowMinutes);

            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                limiter.OnRejected = async (context, cancellationToken) =>
                {
                    TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                        ? delay
                        : null;

                    await WriteTooManyRequestsAsync(context.HttpContext, retryAfter, cancellationToken);
                };

                // Applies to every request. Signed-in users are counted by their own id; everyone else by address.
                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var permits = IsSignedIn(context) ? settings.UserRequestsPerWindow : settings.AnonymousRequestsPerWindow;
                    return CreateFixedWindow(settings.Enabled, GetPartitionKey(context), permits, window);
                });

                limiter.AddPolicy(RateLimitPolicies.Auth, context =>
                    CreateFixedWindow(settings.Enabled, GetClientIpKey(context), settings.AuthRequestsPerWindow, window));

                limiter.AddPolicy(RateLimitPolicies.PasswordReset, context =>
                    CreateFixedWindow(settings.Enabled, GetClientIpKey(context), settings.PasswordResetRequestsPerWindow, passwordResetWindow));

                limiter.AddPolicy(RateLimitPolicies.Upload, context =>
                    CreateFixedWindow(settings.Enabled, GetPartitionKey(context), settings.UploadRequestsPerWindow, uploadWindow));
            });

            return services;
        }

        public static string GetPartitionKey(HttpContext context)
        {
            return IsSignedIn(context)
                ? $"user:{context.User.FindFirstValue("sub")}"
                : GetClientIpKey(context);
        }

        public static string GetClientIpKey(HttpContext context)
        {
            return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        // Writes the 429 response as a JSON error object
        public static async Task WriteTooManyRequestsAsync(HttpContext context, TimeSpan? retryAfter, CancellationToken cancellationToken = default)
        {
            var response = context.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;

            int? retryAfterSeconds = null;
            if (retryAfter is { } delay)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds));
                response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
            }

            await response.WriteAsJsonAsync(
                new
                {
                    message = "Too many requests. Please wait a moment and try again.",
                    retryAfterSeconds
                },
                cancellationToken);
        }

        private static bool IsSignedIn(HttpContext context)
        {
            return context.User.Identity?.IsAuthenticated == true
                && !string.IsNullOrWhiteSpace(context.User.FindFirstValue("sub"));
        }

        private static RateLimitPartition<string> CreateFixedWindow(bool enabled, string key, int permitLimit, TimeSpan window)
        {
            if (!enabled)
            {
                return RateLimitPartition.GetNoLimiter(key);
            }

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }
    }
}
