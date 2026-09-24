using System.Net.Http.Json;

namespace frontend.Services
{
    public record ClientProfile(int ClientId, string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string? RiskProfile, string Status, DateTime CreatedAt);

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
                var url = string.IsNullOrWhiteSpace(search) ? "Client" : $"Client?search={Uri.EscapeDataString(search)}";
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
    }
}
