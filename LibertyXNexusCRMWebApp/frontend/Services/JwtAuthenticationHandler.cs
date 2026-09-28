using System.Net.Http.Headers;

namespace frontend.Services
{
    public class JwtAuthenticationHandler : DelegatingHandler
    {
        private readonly TokenStorageService _tokenStorage;
        private readonly ILogger<JwtAuthenticationHandler> _logger;

        public JwtAuthenticationHandler(TokenStorageService tokenStorage, ILogger<JwtAuthenticationHandler> logger)
        {
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the JWT that was stored during login.
            var token = await _tokenStorage.GetTokenAsync();


            if (!string.IsNullOrWhiteSpace(token))
            {
                // Add:
                //
                // Authorization: Bearer <JWT>
                //
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);

                _logger.LogInformation(
                    "JWT attached to API request: {Method} {Url}",
                    request.Method,
                    request.RequestUri);
            }
            else
            {
                _logger.LogWarning(
                    "No JWT available for API request: {Method} {Url}",
                    request.Method,
                    request.RequestUri);
            }


            return await base.SendAsync(
                request,
                cancellationToken);
        }
    }
    
}
