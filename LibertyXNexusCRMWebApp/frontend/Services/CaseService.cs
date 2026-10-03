namespace frontend.Services
{
    public record CaseSummary(
            int CaseId,
            int PolicyId,
            string? PolicyName,
            string Status,
            string? Notes,
            DateTime CreatedAt,
            DateTime? UpdatedAt
        );

    public class CaseService
    {
        private readonly HttpClient _http;

        public CaseService(HttpClient http)
        {
            _http = http;
        }

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
