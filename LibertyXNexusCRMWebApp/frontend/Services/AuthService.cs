using System.Net.Http.Json;

namespace frontend.Services
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string Password, string Token);
    public record AuthResult(bool Success, string Message, string? Role, int? Id, string? FirstName, string? LastName, string? Email);

    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("Auth/login", new LoginRequest(email, password));
            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            return result ?? new AuthResult(false, "Unable to reach server", null, null, null, null, null);
        }

        public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string? phone, string? identityNumber, string password, string token)
        {
            var response = await _http.PostAsJsonAsync("Auth/register", new RegisterRequest(firstName, lastName, email, phone, identityNumber, password, token));
            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            return result ?? new AuthResult(false, "Unable to reach server", null, null, null, null, null);
        }
    }
}
