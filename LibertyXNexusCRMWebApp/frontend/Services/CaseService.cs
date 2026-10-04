namespace frontend.Services
{
    /// <summary>
    /// The info about a case that we show on the client's side.
    /// </summary>
    public record CaseSummary(
            int CaseId,
            int PolicyId,
            string? PolicyName,
            string Status,
            string? Notes,
            DateTime CreatedAt,
            DateTime? UpdatedAt
        );

    /// <summary>
    /// This service gets a client's cases from the API.
    /// </summary>
    public class CaseService
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Sets up the service with the HttpClient.
        /// </summary>
        public CaseService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets all the cases for a client from the API.
        /// </summary>
        public async Task<(List<CaseSummary> Cases, string? Error)>
            GetForClientAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"Cases/client/{clientId}");

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<CaseSummary>(),await ReadErrorAsync(response));
                }

                var cases =await response.Content.ReadFromJsonAsync<List<CaseSummary>>();

                return (cases ?? new List<CaseSummary>(),null);
            }
            catch (HttpRequestException)
            {
                return (new List<CaseSummary>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<CaseSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the error message the API sent back.
        /// </summary>
        private static async Task<string> ReadErrorAsync(
            HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                return $"API returned HTTP {(int)response.StatusCode}: {body}";
            }

            return $"The server reported an error (status {(int)response.StatusCode}).";
        }
    
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
