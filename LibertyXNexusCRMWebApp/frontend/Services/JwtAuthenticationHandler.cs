using System.Net.Http.Headers;

namespace frontend.Services
{
    /// <summary>
    /// Handles the inserting jwt into headers and storing them
    /// </summary>
    public class JwtAuthenticationHandler : DelegatingHandler
    {
        private readonly TokenStorageService _tokenStorage;
        private readonly ILogger<JwtAuthenticationHandler> _logger;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="JwtAuthenticationHandler"/> class. 
        /// </summary>
        /// <param name="tokenStorage"></param>
        /// <param name="logger"></param>
        public JwtAuthenticationHandler(TokenStorageService tokenStorage, ILogger<JwtAuthenticationHandler> logger)
        {
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Attaches the jwt token to the header
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
