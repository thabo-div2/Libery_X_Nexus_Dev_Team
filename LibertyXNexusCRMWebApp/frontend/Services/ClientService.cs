using System.Net;
using System.Net.Http.Json;

namespace frontend.Services
{
    public record ClientProfile(int ClientId, string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string? RiskProfile, string Status, DateTime CreatedAt, int? AdvisorId, string? AdvisorName);

    public class ClientService
    {
        private readonly HttpClient _http;

        public ClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<ClientProfile> Clients, string? Error)> SearchAsync(string? search, int? advisorId = null)
        {
            try
            {
                var queryParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    queryParts.Add($"search={Uri.EscapeDataString(search)}");
                }
                if (advisorId is not null)
                {
                    queryParts.Add($"advisorId={advisorId.Value}");
                }

                var url = queryParts.Count == 0 ? "Client" : $"Client?{string.Join("&", queryParts)}";
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
                var response = await _http.GetAsync($"Client/{id}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null, "That client could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (null, $"The server reported an error (status {(int)response.StatusCode}).");
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
    }
}
