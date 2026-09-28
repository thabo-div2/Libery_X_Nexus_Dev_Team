using System.Net;
using System.Net.Http.Json;

namespace frontend.Services
{
    public record ClientProfile(
        int ClientId,
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        string? IdentityNumber,
        string? RiskProfile,
        string Status,
        DateTime CreatedAt,
        int? AdvisorId,
        string? AdvisorName);

    public class ClientService
    {
        private readonly HttpClient _http;

        public ClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<ClientProfile> Clients, string? Error)> SearchAsync(string? search)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(search) ? "clients" : $"clients?search={Uri.EscapeDataString(search)}";
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ClientProfile>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<ClientProfile>>();
                return (result ?? new List<ClientProfile>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<ClientProfile>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ClientProfile>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(ClientProfile? Client, string? Error)> GetByIdAsync(int id)
        {
            try
            {
                var response = await _http.GetAsync($"clients/{id}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null, "That client could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await ReadErrorAsync(response);
                    return (null, error);
                }

                var result = await response.Content.ReadFromJsonAsync<ClientProfile>();

                return (result, result is null ? "The server sent back an unexpected response." : null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                return $"API returned HTTP {(int)response.StatusCode}: {body}";
            }

            return $"The server reported an error (status {(int)response.StatusCode}.";
        }
    }
}
