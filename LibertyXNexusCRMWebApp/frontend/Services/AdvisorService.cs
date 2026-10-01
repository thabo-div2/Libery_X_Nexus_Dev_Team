using Shared.DTOs.Advisor;

namespace frontend.Services
{
    public class AdvisorService
    {
        private readonly HttpClient _http;

        public AdvisorService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(AdvisorDashboardDto? Dashboard, string? Error)> GetDashboardAsync()
        {
            try
            {
                var response = await _http.GetAsync("advisor/dashboard");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(errorBody))
                    {
                        return (null, $"API returned HTTP {(int)response.StatusCode}: {errorBody}.");
                    }

                    return (null, $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var dashboard = await response.Content.ReadFromJsonAsync<AdvisorDashboardDto>();

                if (dashboard is null)
                {
                    return (null, "The API returned an empty dashboard.");
                }

                return (dashboard, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the API.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }
    }
}
