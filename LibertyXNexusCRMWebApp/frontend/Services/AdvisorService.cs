using Shared.DTOs.Advisor;

namespace frontend.Services
{
    /// <summary>
    /// Service for interacting with the advisor-related API endpoints.
    /// </summary>
    public class AdvisorService
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvisorService"/> class with the specified <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="http"></param>
        public AdvisorService(HttpClient http)
        {
            _http = http;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the advisor dashboard data from the API.
        /// </summary>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
